using System;
using System.Collections;
using System.IO;
using g3;

namespace gs
{
	public class ARKitStreamPlayback
	{
		ARKitStream source;
		VideoStream video_source;
		int N = 0;
		int cur_frame = 0;
		int cur_video_frame = 0;

		float sourceTimeShift = 0;
		float liveTimeShift = 0;

		// add handlers to play result
		public Action<float> NewFrameF = (time) => { };
		public Action<Vector3f, Quaternionf> UpdateCameraF = (pos, orientation) => { };
		public Action<Vector3f, Vector2f, Colorf> EmitSamplePointF = (pos, screenPos, color) => { };

		public Action<ARKitStream.PlaneChange, ARKitStream.ARKitPlane> PlaneChangeF = (changetype, info) => {};

		public Action<VideoFrame> EmitVideoFrameF = (frame) => { };

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
		}
		public void Stop(bool reset) {
			if (reset)
				NewFrameF(0);
		}


		public void Update(float curTime) {
			curTime -= liveTimeShift;

			while (cur_frame < N && 
			       (source.Frames[cur_frame].realTime-sourceTimeShift) < (curTime) ) 
			{
				//UnityEngine.Debug.Log("Playing frame " + curFrame + " " + (source.Frames[curFrame].realTime - sourceTimeShift) );

				var frame = source.Frames[cur_frame++];

				NewFrameF(frame.realTime - sourceTimeShift);
				UpdateCameraF(frame.camPos, frame.camOrientation);

				foreach (var pt in frame.SamplePoints)
					EmitSamplePointF(pt.pos, pt.screenPos, pt.color);
			}


			if ( video_source != null ) {
				int send_frame = -1;
				if (cur_video_frame < video_source.Frames.Count && 
				    (video_source.Frames[cur_video_frame].realTime - sourceTimeShift) < (curTime))
				{
					send_frame = cur_video_frame;
					cur_video_frame++;
				}
				if (send_frame >= 0)
					EmitVideoFrameF(video_source.Frames[send_frame]);
			}

		}

	}
}