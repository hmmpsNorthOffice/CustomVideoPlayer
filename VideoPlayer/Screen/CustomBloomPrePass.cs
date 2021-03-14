﻿using System;
using System.Collections.Generic;
using System.Linq;
using BS_Utils.Utilities;
using IPA.Utilities;
using UnityEngine;

namespace CustomVideoPlayer
{
	internal class CustomBloomPrePass : MonoBehaviour, CameraRenderCallbacksManager.ICameraRenderCallbacks
	{
		//Most cameras have their own BloomPrePass, so save one for each camera and use that when rendering the bloom for the camera
		private readonly Dictionary<Camera, BloomPrePass?> _bloomPrePassDict = new Dictionary<Camera, BloomPrePass?>();
		private readonly Dictionary<Camera, BloomPrePassRendererSO> _bloomPrePassRendererDict = new Dictionary<Camera, BloomPrePassRendererSO>();
		private readonly Dictionary<Camera, BloomPrePassRenderDataSO.Data> _bloomPrePassRenderDataDict = new Dictionary<Camera, BloomPrePassRenderDataSO.Data>();
		private readonly Dictionary<Camera, IBloomPrePassParams> _bloomPrePassParamsDict = new Dictionary<Camera, IBloomPrePassParams>();

		private Material _additiveMaterial = null!;
		private KawaseBlurRendererSO _kawaseBlurRenderer = null!;

		private Renderer _renderer = null!;
		private Mesh _mesh = null!;
		private static readonly int Alpha = Shader.PropertyToID("_Alpha");
		private const int DOWNSAMPLE = 2;
		private const float BLOOM_BOOST_FACTOR = 0.11f; // 0.22f;  // orig 0.11f;
		private float? _bloomIntensityConfigSetting;
		private Vector2 _screnDimensions;

		private void Start()
		{
			UpdateMesh();
			_renderer = GetComponent<Renderer>();

			_kawaseBlurRenderer = Resources.FindObjectsOfTypeAll<KawaseBlurRendererSO>().First();
			_additiveMaterial = new Material(Shader.Find("Hidden/BlitAdd"));
			_additiveMaterial.SetFloat(Alpha, 1f);

			BSEvents.menuSceneLoaded += UpdateMesh;
			BSEvents.gameSceneLoaded += UpdateMesh;
			BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoaded;
		}

		public void UpdateMesh()
		{
			_mesh = GetComponent<MeshFilter>().mesh;
		}

		public void UpdateScreenDimensions(float width, float height)
		{
			_screnDimensions = new Vector2(width, height);
		}

		public void SetBloomIntensityConfigSetting(float? bloomIntensity)
		{
			_bloomIntensityConfigSetting = bloomIntensity;
		}

		private float GetBloomBoost(Camera camera)
		{
			var fov = camera.fieldOfView;

			//Base calculation scales down with screen width and up with distance
			var boost = (BLOOM_BOOST_FACTOR / (float)Math.Sqrt(_screnDimensions.x / GetCameraDistance(camera)));
			// what if we tried it based on the inverse of the ((Screen Area)^2)?  ... 
			//var boost = (BLOOM_BOOST_FACTOR / (float)(_screnDimensions.x * _screnDimensions.y * _screnDimensions.x * _screnDimensions.y));
			// var boost = (BLOOM_BOOST_FACTOR / (float)Math.Sqrt((_screnDimensions.x*_screnDimensions.y) / GetCameraDistance(camera)));

			//Apply map/user setting on top

			if (_bloomIntensityConfigSetting == null)  // (vz changed != to ==)
			{
				_bloomIntensityConfigSetting = Math.Min(2f, Math.Max(0f, _bloomIntensityConfigSetting.Value));  // limits range of json input from 0 to 2
				boost *= (float)Math.Sqrt(_bloomIntensityConfigSetting.Value);
				//Plugin.Logger.Debug("value init from json");  //  this gets fired if conditional left as is ...
			}
			else
			{
				boost *= (float)Math.Sqrt(_bloomIntensityConfigSetting.Value / 100f);  // slider values currently set 0-200
				//Plugin.Logger.Debug("value init from slider");
			}

			//Mitigate extreme amounts of bloom at the edges of the camera frustum when not looking directly at the screen
			var targetDirection = gameObject.transform.position - camera.transform.position;
			var angle = Vector3.Angle(targetDirection, camera.transform.forward);
			angle /= (fov / 2);
			const float threshold = 0.3f;
			//Prevent brightness from fluctuating when looking close to the center
			angle = Math.Max(threshold, angle);
			boost /= ((angle + (1 - threshold)) * (fov / 100f));

			//Adjust for FoV
			boost *= fov / 100f;

		//	Plugin.Logger.Debug("logging boost factor = " + boost.ToString());
			return boost;
		}

		private float GetCameraDistance(Camera camera)
		{
			return (gameObject.transform.position - camera.transform.position).magnitude;
		}

		private void GetPrivateFields(Camera camera)
		{
			if (_bloomPrePassDict.ContainsKey(camera))
			{
				return;
			}


			var bloomPrePass = camera.GetComponent<BloomPrePass>();
			_bloomPrePassDict.Add(camera, bloomPrePass);
			if (bloomPrePass == null)
			{
				Plugin.Logger.Info($"Failed to find BloomPrePass for camera {camera.name}");
				return;
			}

			_bloomPrePassRendererDict.Add(camera, bloomPrePass.GetField<BloomPrePassRendererSO, BloomPrePass>("_bloomPrepassRenderer"));
			_bloomPrePassRenderDataDict.Add(camera, bloomPrePass.GetField<BloomPrePassRenderDataSO.Data, BloomPrePass>("_renderData"));
			var effectsContainer = bloomPrePass.GetField<BloomPrePassEffectContainerSO, BloomPrePass>("_bloomPrePassEffectContainer");
			_bloomPrePassParamsDict.Add(camera, effectsContainer.GetField<BloomPrePassEffectSO, BloomPrePassEffectContainerSO>("_bloomPrePassEffect"));
		}

		public void OnCameraPostRender(Camera camera)
		{
			//intentionally empty
		}

		public void OnCameraPreRender(Camera camera)
		{
			if (camera == null)
			{
				return;
			}

			try
			{
				if(VideoMenu.BloomOn) ApplyBloomEffect(camera);      // global enable/disable patch 
			}
			catch (Exception e)
			{
				Plugin.Logger.Error(e);
				var result = _bloomPrePassDict.TryGetValue(camera, out var bloomPrePass);
				if (result == false)
				{
					_bloomPrePassDict.Add(camera, null);
				}

				if (bloomPrePass != null)
				{
					_bloomPrePassDict[camera] = null;
				}
			}
		}

		private void ApplyBloomEffect(Camera camera)
		{
			//TODO Fix SmoothCamera instead of skipping. Current workaround is to use CameraPlus instead. Investigate what BloomPrePassRendererSO does differently
			//Mirror cam has no BloomPrePass
			if (camera.name == "SmoothCamera" || camera.name.StartsWith("MirrorCam"))
			{
				Plugin.Logger.Debug("camera name == smooth || Mirror");
				return;
			}

			try
			{
				GetPrivateFields(camera);
			}
			catch (Exception e)
			{
				Plugin.Logger.Error(e);
				_bloomPrePassDict.Add(camera, null);
			}

			_bloomPrePassDict.TryGetValue(camera, out var bloomPrePass);
			if (bloomPrePass == null)
			{
				Plugin.Logger.Debug("bloomPrePass == null");
				return;
			}

			var rendererFound = _bloomPrePassRendererDict.TryGetValue(camera, out var bloomPrePassRenderer);
			var paramsFound = _bloomPrePassParamsDict.TryGetValue(camera, out var bloomPrePassParams);
			var renderDataFound = _bloomPrePassRenderDataDict.TryGetValue(camera, out var bloomPrePassRenderData);

			//Never the case in my testing, but better safe than sorry
			if (!rendererFound || !paramsFound || !renderDataFound)
			{
				return;
			}

			var sRGBWrite = GL.sRGBWrite;
			GL.sRGBWrite = false;

			bloomPrePassRenderer.GetCameraParams(camera, out var projectionMatrix, out _, out var stereoCameraEyeOffset);

			//The next few lines are taken from bloomPrePassRenderer.RenderAndSetData()
			var textureToScreenRatio = new Vector2
			{
				x = Mathf.Clamp01((float)(1.0 / ((double)Mathf.Tan((float)(bloomPrePassParams.fov.x * 0.5 * (Math.PI / 180.0))) * projectionMatrix.m00))),
				y = Mathf.Clamp01((float)(1.0 / ((double)Mathf.Tan((float)(bloomPrePassParams.fov.y * 0.5 * (Math.PI / 180.0))) * projectionMatrix.m11)))
			};
			projectionMatrix.m00 *= textureToScreenRatio.x;
			projectionMatrix.m02 *= textureToScreenRatio.x;
			projectionMatrix.m11 *= textureToScreenRatio.y;
			projectionMatrix.m12 *= textureToScreenRatio.y;

			RenderTexture temporary = RenderTexture.GetTemporary(bloomPrePassParams.textureWidth, bloomPrePassParams.textureHeight, 0, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.Linear);
			Graphics.SetRenderTarget(temporary);
			GL.Clear(true, true, Color.black);

			GL.PushMatrix();
			GL.LoadProjectionMatrix(projectionMatrix);
			_renderer.material.SetPass(0);
			var transformTemp = transform;
			Graphics.DrawMeshNow(_mesh, Matrix4x4.TRS(transformTemp.position, transformTemp.rotation, transformTemp.lossyScale));
			GL.PopMatrix();

			var boost = GetBloomBoost(camera);
			RenderTexture blur2 = RenderTexture.GetTemporary(bloomPrePassParams.textureWidth >> DOWNSAMPLE, bloomPrePassParams.textureHeight >> DOWNSAMPLE,
				0, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.Linear);
			DoubleBlur(temporary, blur2,
				KawaseBlurRendererSO.KernelSize.Kernel127, boost,
				KawaseBlurRendererSO.KernelSize.Kernel35, boost, 0.5f, DOWNSAMPLE);

			Graphics.Blit(blur2, bloomPrePassRenderData.bloomPrePassRenderTexture, _additiveMaterial);

			RenderTexture.ReleaseTemporary(temporary);
			RenderTexture.ReleaseTemporary(blur2);

		//	BloomPrePassRendererSO.SetDataToShaders(stereoCameraEyeOffset, textureToScreenRatio, bloomPrePassRenderData.bloomPrePassRenderTexture);
			BloomPrePassRendererSO.SetDataToShaders(stereoCameraEyeOffset, textureToScreenRatio, bloomPrePassRenderData.bloomPrePassRenderTexture, bloomPrePassRenderData.toneMapping);
			GL.sRGBWrite = sRGBWrite;
		}

		public void OnMenuSceneLoaded(ScenesTransitionSetupDataSO scenesTransitionSetupDataSo)
		{
			UpdateMesh();
		}

		private void OnWillRenderObject()
		{
			CameraRenderCallbacksManager.RegisterForCameraCallbacks(Camera.current, this);
		}

		public void OnDisable()
		{
			CameraRenderCallbacksManager.UnregisterFromCameraCallbacks(this);
		}

		private void OnDestroy()
		{
			CameraRenderCallbacksManager.UnregisterFromCameraCallbacks(this);
			BSEvents.menuSceneLoaded -= UpdateMesh;
			BSEvents.gameSceneLoaded -= UpdateMesh;
			BSEvents.lateMenuSceneLoadedFresh -= OnMenuSceneLoaded;
		}

		private void DoubleBlur(RenderTexture src, RenderTexture dest, KawaseBlurRendererSO.KernelSize kernelSize0, float boost0, KawaseBlurRendererSO.KernelSize kernelSize1, float boost1, float secondBlurAlpha, int downsample)
		{
			int[] blurKernel = _kawaseBlurRenderer.GetBlurKernel(kernelSize0);
			int[] blurKernel2 = _kawaseBlurRenderer.GetBlurKernel(kernelSize1);
			var num = 0;
			while (num < blurKernel.Length && num < blurKernel2.Length && blurKernel[num] == blurKernel2[num])
			{
				num++;
			}
			var width = src.width >> downsample;
			var height = src.height >> downsample;
			var descriptor = src.descriptor;
			descriptor.depthBufferBits = 0;
			descriptor.width = width;
			descriptor.height = height;
			RenderTexture temporary = RenderTexture.GetTemporary(descriptor);
			_kawaseBlurRenderer.Blur(src, temporary, blurKernel, 0f, downsample, 0, num, 0f, 1f, false, true, KawaseBlurRendererSO.WeightsType.None);
			_kawaseBlurRenderer.Blur(temporary, dest, blurKernel, boost0, 0, num, blurKernel.Length - num, 0f, 1f, false, true, KawaseBlurRendererSO.WeightsType.None);
			_kawaseBlurRenderer.Blur(temporary, dest, blurKernel2, boost1, 0, num, blurKernel2.Length - num, 0f, secondBlurAlpha, true, true, KawaseBlurRendererSO.WeightsType.None);
			RenderTexture.ReleaseTemporary(temporary);
		}
	}
}