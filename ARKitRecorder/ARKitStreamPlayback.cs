// Copyright (c) Ryan Schmidt - rms@gradientspace.com - twitter @rms80
// released under the MIT License (see LICENSE file)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace gs
{
	public class ARKitStreamPlayback
	{
		ARKitStream source;
		VideoStream video_source;
		int N = 0;
		int cur_frame = 0;
		int cur_video_frame = 0;

		Vector3 lastCamPos;
		Quaternion lastCamRotate;
		bool camera_initialized = false;

		float sourceTimeShift = 0;
		float liveTimeShift = 0;


        /// <summary>
        /// These handler functions are called for different events in the data stream.
        /// Replace them with your own implementations to do more interesting things.
        /// </summary>

		// Called once for each new frame, with "realtime" time (ie seconds since start of stream)
		public Action<float> NewFrameF = (time) => { };

        // Called once per frame, with the set of Point Cloud points that ARKit provided for that frame
		public Action<ARKitStream.ARKitPoint[]> NewFramePointsF = null;

        // Called with same data as NewFramePointsF, but for each point separately
        public Action<Vector3, Vector2, Color> EmitSamplePointF = null;

        // Called with new camera position/orientation. Although the values will only change on each NewFrameF(),
        // we call this every Update() because otherwise the ARKit plugin will reset the camera
		public Action<Vector3, Quaternion> UpdateCameraF = (pos, orientation) => { };

        // Called each NewFrameF() with the image from the video stream, if it is available
        public Action<VideoFrame> EmitVideoFrameF = null;

        public Action<ARKitStream.PlaneChange, ARKitStream.ARKitPlane> PlaneChangeF = (changetype, info) => { };



        public void SetSource(ARKitStream src, VideoStream video_src) {
			source = src;
			N = source.Frames.Count;

			video_source = video_src;
		}

		public void Start(float curTime) {
			cur_frame = 0;
			sourceTimeShift = source.Frames[cur_frame].realTime;
			liveTimeShift = curTime;

			cur_video_frame = 0;

			camera_initialized = false;
		}
		public void Stop(bool reset) {
			if (reset)
				NewFrameF(0);
		}


		public void Update(float curTime) {
			curTime -= liveTimeShift;

			// try to emit new frames/pointsets in (timeshifted) real time
			while (cur_frame < N && 
			       (source.Frames[cur_frame].realTime-sourceTimeShift) < (curTime) ) 
			{
				//UnityEngine.Debug.Log("Playing frame " + curFrame + " " + (source.Frames[curFrame].realTime - sourceTimeShift) );

				var frame = source.Frames[cur_frame++];

				NewFrameF(frame.realTime - sourceTimeShift);
				lastCamPos = frame.camPos;
				lastCamRotate = frame.camOrientation;
				camera_initialized = true;

                // emit scan points
				if (NewFramePointsF != null) {
					NewFramePointsF(frame.SamplePoints.ToArray());
				}
				if (EmitSamplePointF != null) {
					foreach (var pt in frame.SamplePoints)
						EmitSamplePointF(pt.pos, pt.screenPos, pt.color);
				}

                // emit plane changes
                if (PlaneChangeF != null) {
                    foreach (var plane in frame.RemovedPlanes) {
                        PlaneChangeF(ARKitStream.PlaneChange.Removed, plane);
                    }
                    foreach (var plane in frame.AddedPlanes) {
                        PlaneChangeF(ARKitStream.PlaneChange.Added, plane);
                    }
                    foreach (var plane in frame.UpdatedPlanes) {
                        PlaneChangeF(ARKitStream.PlaneChange.Updated, plane);
                    }
                }
			}

			// update video frame if we have a new one for this time
			if ( video_source != null ) {
				int send_frame = -1;
				if (cur_video_frame < video_source.Frames.Count && 
				    (video_source.Frames[cur_video_frame].realTime - sourceTimeShift) < (curTime))
				{
					send_frame = cur_video_frame;
					cur_video_frame++;
				}
				if (send_frame >= 0 && EmitVideoFrameF != null)
					EmitVideoFrameF(video_source.Frames[send_frame]);
			}

			// we update camera every frame
			if ( camera_initialized && UpdateCameraF != null )
				UpdateCameraF(lastCamPos, lastCamRotate);
		}

	}
}