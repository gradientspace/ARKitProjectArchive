using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.XR.iOS
{

    public class UnityARVideo : MonoBehaviour
    {
        public Material m_ClearMaterial;

        private CommandBuffer m_VideoCommandBuffer;
        private Texture2D _videoTextureY;
        private Texture2D _videoTextureCbCr;

		private bool bCommandBufferInitialized;

		private float fTexCoordScale;
		private ScreenOrientation screenOrientation;


		public void Start()
		{
			fTexCoordScale = 1.0f;
			screenOrientation = ScreenOrientation.LandscapeLeft;
			UnityARSessionNativeInterface.ARFrameUpdatedEvent += UpdateFrame;
			bCommandBufferInitialized = false;

			// [RMS] initialize image texture (TODO: handle orientation change...)
			initialize_image_tex();
		}

		void UpdateFrame(UnityARCamera cam)
		{
			fTexCoordScale = cam.videoParams.texCoordScale;
			screenOrientation = (ScreenOrientation) cam.videoParams.screenOrientation;

		}

		void InitializeCommandBuffer()
		{
			m_VideoCommandBuffer = new CommandBuffer(); 
			m_VideoCommandBuffer.Blit(null, BuiltinRenderTextureType.CurrentActive, m_ClearMaterial);
			// [RMS] blit camera image to our own RenderTexture so we can use later
			m_VideoCommandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, m_BlitTarget);
			GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeForwardOpaque, m_VideoCommandBuffer);
			bCommandBufferInitialized = true;

		}

		void OnDestroy()
		{
			GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, m_VideoCommandBuffer);
			UnityARSessionNativeInterface.ARFrameUpdatedEvent -= UpdateFrame;
			bCommandBufferInitialized = false;
		}

		public void OnPostRender()
		{
			// [RMS] read out image texture we saved in command buffer
			// [TODO] ony 
			copy_image_tex();
		}

#if !UNITY_EDITOR

        public void OnPreRender()
        {
			ARTextureHandles handles = UnityARSessionNativeInterface.GetARSessionNativeInterface ().GetARVideoTextureHandles();
            if (handles.textureY == System.IntPtr.Zero || handles.textureCbCr == System.IntPtr.Zero)
            {
                return;
            }

            if (!bCommandBufferInitialized) {
                InitializeCommandBuffer ();
            }

            Resolution currentResolution = Screen.currentResolution;

            // Texture Y
            _videoTextureY = Texture2D.CreateExternalTexture(currentResolution.width, currentResolution.height,
                TextureFormat.R8, false, false, (System.IntPtr)handles.textureY);
            _videoTextureY.filterMode = FilterMode.Bilinear;
            _videoTextureY.wrapMode = TextureWrapMode.Repeat;
            _videoTextureY.UpdateExternalTexture(handles.textureY);

            // Texture CbCr
            _videoTextureCbCr = Texture2D.CreateExternalTexture(currentResolution.width, currentResolution.height,
                TextureFormat.RG16, false, false, (System.IntPtr)handles.textureCbCr);
            _videoTextureCbCr.filterMode = FilterMode.Bilinear;
            _videoTextureCbCr.wrapMode = TextureWrapMode.Repeat;
            _videoTextureCbCr.UpdateExternalTexture(handles.textureCbCr);

            m_ClearMaterial.SetTexture("_textureY", _videoTextureY);
            m_ClearMaterial.SetTexture("_textureCbCr", _videoTextureCbCr);
            int isPortrait = 0;

            float rotation = 0;
            if (screenOrientation == ScreenOrientation.Portrait) {
                rotation = -90;
                isPortrait = 1;
            }
            else if (screenOrientation == ScreenOrientation.PortraitUpsideDown) {
                rotation = 90;
                isPortrait = 1;
            }
            else if (screenOrientation == ScreenOrientation.LandscapeRight) {
                rotation = -180;
            }
            Matrix4x4 m = Matrix4x4.TRS (Vector3.zero, Quaternion.Euler(0.0f, 0.0f, rotation), Vector3.one);
            m_ClearMaterial.SetMatrix("_TextureRotation", m);
			m_ClearMaterial.SetFloat("_texCoordScale", fTexCoordScale);
            m_ClearMaterial.SetInt("_isPortrait", isPortrait);
        }
#else

		public void SetYTexure(Texture2D YTex)
		{
			_videoTextureY = YTex;
		}

		public void SetUVTexure(Texture2D UVTex)
		{
			_videoTextureCbCr = UVTex;
		}

		public void OnPreRender()
		{

			if (!bCommandBufferInitialized) {
				InitializeCommandBuffer ();
			}

			m_ClearMaterial.SetTexture("_textureY", _videoTextureY);
			m_ClearMaterial.SetTexture("_textureCbCr", _videoTextureCbCr);
			int isPortrait = 0;

			float rotation = 0;
			if (screenOrientation == ScreenOrientation.Portrait) {
				rotation = -90;
				isPortrait = 1;
			}
			else if (screenOrientation == ScreenOrientation.PortraitUpsideDown) {
				rotation = 90;
				isPortrait = 1;
			}
			else if (screenOrientation == ScreenOrientation.LandscapeRight) {
				rotation = -180;
			}

			Matrix4x4 m = Matrix4x4.TRS (Vector3.zero, Quaternion.Euler(0.0f, 0.0f, rotation), Vector3.one);
			m_ClearMaterial.SetMatrix("_TextureRotation", m);
			m_ClearMaterial.SetFloat("_texCoordScale", fTexCoordScale);
			m_ClearMaterial.SetInt("_isPortrait", isPortrait);


		}

#endif



		// [RMS] keep a copy of current video frame in a format we can query
		RenderTexture m_BlitTarget;
		Texture2D m_ImageTex;

		public Color QueryPixel(float sx, float sy) {
			if (m_ImageTex == null)
				return Color.green;

			return m_ImageTex.GetPixel((int)sx, m_ImageTex.height - 1 - (int)sy);
		}
		public Texture2D CurrentFrameTex {
			get { return m_ImageTex; }
		}


		void initialize_image_tex() {
			Resolution currentResolution = Screen.currentResolution;
			m_BlitTarget = new RenderTexture(currentResolution.width, currentResolution.height, 0,
											  RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			m_ImageTex = new Texture2D(currentResolution.width, currentResolution.height,
									   TextureFormat.ARGB32, false);
		}

		void copy_image_tex()
		{
			RenderTexture currentRT = RenderTexture.active;

			// copy RenderTexture that we blitted camera image to into a Texture2D
			RenderTexture.active = m_BlitTarget;
			m_ImageTex.ReadPixels(new Rect(0, 0, m_BlitTarget.width, m_BlitTarget.height), 0, 0);
			m_ImageTex.Apply();

			RenderTexture.active = currentRT;
		}



    }
}
