// Copyright (c) Ryan Schmidt - rms@gradientspace.com - twitter @rms80
// released under the MIT License (see LICENSE file)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR.iOS;


namespace gs
{

	public class ARKitStreamRecorder
	{
		public static int TargetVideoFrameRate = 10;

		public static bool EnableSaveVideoStream = false;

		public static Action<string> ErrorMessageF = Debug.Log;


		public static bool IsRecording {
			get { return SaveStream != null; }
		}


		static gs.ARKitStream SaveStream = null;
		static gs.VideoStream SaveVideoStream = null;
		static GameObject VideoGO = null;  // temp GO used during video recording to process each frame


		public static void StartRecording()
		{
			register_events();

			DateTime date = DateTime.Now;
			string prefix = date.Day + "-" + date.Month + "-" + date.Year + "-" + date.Hour + "-" + date.Minute + "-" + date.Second;
			string sName = Path.Combine(Application.persistentDataPath, "ARStream_" + prefix + ".txt");
			var stream = new gs.ARKitStream();
			try {
				stream.InitializeWrite(sName);
				SaveStream = stream;
			} catch (Exception e) {
				ErrorMessageF("Error creating stream file " + sName + " : " + e.Message);
			}

			if (EnableSaveVideoStream) {
				VideoGO = new GameObject("gs_stream_recorder");
				VideoGO.AddComponent<RecorderFrameProcessing>().PerFrameFunc = per_frame_processing;

				string sVideoName = Path.Combine(Application.persistentDataPath, "VideoStream_" + prefix + ".bin");
				var video_stream = new gs.VideoStream();
				try {
					video_stream.InitializeWrite(sVideoName);
					SaveVideoStream = video_stream;
				} catch (Exception e) {
					ErrorMessageF("Error creating video stream file " + sVideoName + " : " + e.Message);
				}
			}
		}
		public static void StopRecording()
		{
			if (SaveStream != null) {
				SaveStream.Shutdown();
				SaveStream = null;
			}
			if (SaveVideoStream != null) {
				SaveVideoStream.Shutdown();
				SaveVideoStream = null;

				if (VideoGO != null) {
					GameObject.Destroy(VideoGO);
					VideoGO = null;
				}
			}
		}







		static bool events_registered = false;
		public static void register_events()
		{
			if (!events_registered) {
				UnityARSessionNativeInterface.ARAnchorAddedEvent += ARAnchorAdded;
				UnityARSessionNativeInterface.ARAnchorUpdatedEvent += ARAnchorUpdated;
				UnityARSessionNativeInterface.ARAnchorRemovedEvent += ARAnchorRemoved;

				UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;

				events_registered = true;
			}
		}



		// Update is called once per frame
		static float last_video_frame = 0;
		static void per_frame_processing()
		{

			if (SaveStream != null && SaveVideoStream != null) {

				// save video frame if necessary
				if (Time.realtimeSinceStartup - last_video_frame > (1.0f / (float)TargetVideoFrameRate)) {
					UnityARVideo video = Camera.main.gameObject.GetComponent<UnityARVideo>();

					Texture2D tex = video.CurrentFrameTex;
					byte[] pngdata = tex.EncodeToPNG();
					SaveVideoStream.AppendFrame(new gs.VideoFrame() {
						realTime = Time.realtimeSinceStartup,
						width = tex.width, height = tex.height,
						rgb = pngdata
					});
					last_video_frame = Time.realtimeSinceStartup;
				}
			}
		}






		public static void ARFrameUpdated(UnityARCamera arCamera)
		{
			if (SaveStream != null) {

				UnityARVideo video = Camera.main.gameObject.GetComponent<UnityARVideo>();

				// save frame
				SaveStream.BeginFrame(
					Time.realtimeSinceStartup,
					Camera.main.gameObject.transform.position,
					Camera.main.gameObject.transform.rotation);

				// save frame points
				foreach (Vector3 pt in arCamera.pointCloudData) {
					Vector3 screenCoords = Camera.main.WorldToScreenPoint(pt);
					Color c = Color.white;
					if (video != null) {
						c = video.QueryPixel(screenCoords.x, screenCoords.y);
					}

					SaveStream.AppendSample(pt, new Vector2(screenCoords.x,screenCoords.y), c);
				}
			}

		}  // end ARFrameUpdated




		static void ARAnchorAdded(ARPlaneAnchor arAnchor)
		{
			if (SaveStream != null)
				SaveStream.AddPlane(make_planeInfo(arAnchor));
		}
		static void ARAnchorUpdated(ARPlaneAnchor arAnchor)
		{
			if (SaveStream != null)
				SaveStream.UpdatePlane(make_planeInfo(arAnchor));
		}
		static void ARAnchorRemoved(ARPlaneAnchor arAnchor)
		{
			if (SaveStream != null)
				SaveStream.RemovePlane(make_planeInfo(arAnchor));

		}
		static gs.ARKitStream.ARKitPlane make_planeInfo(ARPlaneAnchor anchor)
		{
			var pi = new gs.ARKitStream.ARKitPlane();
			pi.identifier = anchor.identifier;
			pi.position = UnityARMatrixOps.GetPosition(anchor.transform);
			pi.orientation = UnityARMatrixOps.GetRotation(anchor.transform);
			pi.center = anchor.center;
			pi.extents = anchor.extent;
			pi.type = gs.ARKitStream.PlaneType.Horizontal;
			return pi;
		}

	}





	class RecorderFrameProcessing : MonoBehaviour
	{
		public Action PerFrameFunc = () => { };

		public void Update() {
			PerFrameFunc();
		}
	}


}
