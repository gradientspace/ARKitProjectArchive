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

		private UnityARSessionNativeInterface m_Session;


		// [RMS] additions
		public RenderTexture m_BlitTarget;
		public Texture2D m_ImageTex;

		public Color QueryPixel(float sx, float sy) {
			if (m_ImageTex == null)
				return Color.green;			

			return m_ImageTex.GetPixel((int)sx, m_ImageTex.height - 1 - (int)sy);
		}


		void copy_image_tex()
		{
			RenderTexture currentRT = RenderTexture.active;

			// copy RenderTexture that we blitted camera image to into a Texture2D
			RenderTexture.active = m_BlitTarget;
			m_ImageTex.ReadPixels(new Rect(0, 0, m_BlitTarget.width, m_BlitTarget.height), 0, 0);
			m_ImageTex.Apply();

			// pixel testing code
			//Color[] pixels = m_ImageTex.GetPixels();
			//Debug.Log("after blit, pixel 0 is " + pixels[0]);
			//Color c = m_ImageTex.GetPixel(m_BlitTarget.width/2, m_BlitTarget.height/2);
			//Debug.Log("after blit center pixel is " + c);

			RenderTexture.active = currentRT;
		}


#if !UNITY_EDITOR
        private bool bCommandBufferInitialized;

        public void Start()
        {
			m_Session = UnityARSessionNativeInterface.GetARSessionNativeInterface ();
            bCommandBufferInitialized = false;

			Resolution currentResolution = Screen.currentResolution;
			m_BlitTarget = new RenderTexture(currentResolution.width, currentResolution.height, 0,
											  RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			m_ImageTex = new Texture2D(currentResolution.width, currentResolution.height,
									   TextureFormat.ARGB32, false); 
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
        }

        public void OnPreRender()
        {
			ARTextureHandles handles = m_Session.GetARVideoTextureHandles();
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
            if (Screen.orientation == ScreenOrientation.Portrait) {
                rotation = -90;
                isPortrait = 1;
            }
            else if (Screen.orientation == ScreenOrientation.PortraitUpsideDown) {
                rotation = 90;
                isPortrait = 1;
            }
            else if (Screen.orientation == ScreenOrientation.LandscapeRight) {
                rotation = -180;
            }
            Matrix4x4 m = Matrix4x4.TRS (Vector3.zero, Quaternion.Euler(0.0f, 0.0f, rotation), Vector3.one);
            m_ClearMaterial.SetMatrix("_TextureRotation", m);
            m_ClearMaterial.SetFloat("_texCoordScale", m_Session.GetARYUVTexCoordScale());
            m_ClearMaterial.SetInt("_isPortrait", isPortrait);
        }

		public void OnPostRender() 
		{
			copy_image_tex();
		}
#else




		public void Start()
        {
			Resolution currentResolution = Screen.currentResolution;
			m_BlitTarget = new RenderTexture(currentResolution.width, currentResolution.height, 0,
			                                 RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			m_ImageTex = new Texture2D(currentResolution.width, currentResolution.height,
									   TextureFormat.ARGB32, false);                                  


			m_VideoCommandBuffer = new CommandBuffer();
			m_VideoCommandBuffer.Blit(null, BuiltinRenderTextureType.CurrentActive, m_ClearMaterial);
			// [RMS] blit camera image to our own RenderTexture so we can use later
			m_VideoCommandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, m_BlitTarget);
            GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeForwardOpaque, m_VideoCommandBuffer);
        }


		public void OnPreRender()
		{
		}


		public void OnPostRender() 
		{
			copy_image_tex();
		}

        void OnDestroy()
        {
            GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, m_VideoCommandBuffer);
        }

#endif
    }
}
