// Copyright (c) Ryan Schmidt - rms@gradientspace.com - twitter @rms80
// released under the MIT License (see LICENSE file)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace gs
{
	public class ARKitStream
	{

		public enum StreamMode
		{
			MemoryStream,
			WriteToDiskStream
		};
		public StreamMode ActiveMode;

		StreamWriter disk_writer;



		public struct ARKitPoint
		{
			public Vector3 pos;
			public Vector2 screenPos;
			public Color color;
		}



		public enum PlaneChange { Added = 1, Removed = 2, Updated = 3 };
		public enum PlaneType { Horizontal = 1 };
		public struct ARKitPlane
		{
			public string identifier;
			public Vector3 position;
			public Quaternion orientation;
			public PlaneType type;
			public Vector3 center;
			public Vector3 extents;
		}


		public class ARKitFrame
		{
			public float realTime;

			public Vector3 camPos;
			public Quaternion camOrientation;

			public List<ARKitPoint> SamplePoints;
			public List<ARKitPlane> AddedPlanes;
            public List<ARKitPlane> UpdatedPlanes;
            public List<ARKitPlane> RemovedPlanes;
		}


		public List<ARKitFrame> Frames = new List<ARKitFrame>();

		ARKitFrame currentFrame;


		public void InitializeMemory() {
			ActiveMode = StreamMode.MemoryStream;
			Frames = new List<ARKitFrame>();
			currentFrame = null;
		}
		public void InitializeWrite(string sPath) {
			ActiveMode = StreamMode.WriteToDiskStream;
			disk_writer = new StreamWriter(sPath);
		}


		public void InitializeFromFileAscii(string sPath) {
			ActiveMode = StreamMode.MemoryStream;
			Frames = new List<ARKitFrame>();
			currentFrame = null;
			read_ascii(sPath);
		}


		public void Shutdown() {
			if (disk_writer != null) {
				disk_writer.Close();
				disk_writer.Dispose();
			}
		}


		public void BeginFrame(float timeSinceStartup, Vector3 camPos, Quaternion camOrient) {
			currentFrame = new ARKitFrame();
			currentFrame.realTime = timeSinceStartup;
			currentFrame.camPos = camPos;
			currentFrame.camOrientation = camOrient;

			if (ActiveMode == StreamMode.WriteToDiskStream) {
				disk_writer.WriteLine(to_string(currentFrame));

			} else {
				currentFrame.SamplePoints = new List<ARKitPoint>();
				currentFrame.AddedPlanes = new List<ARKitPlane>();
                currentFrame.UpdatedPlanes = new List<ARKitPlane>();
                currentFrame.RemovedPlanes = new List<ARKitPlane>();
				Frames.Add(currentFrame);
			}
		}


		public void AppendSample(Vector3 pos, Vector2 screenPos, Color color)
		{
			ARKitPoint pt = new ARKitPoint();
			pt.pos = pos;
			pt.screenPos = screenPos;
			pt.color = color;

			if (ActiveMode == StreamMode.WriteToDiskStream)
				disk_writer.WriteLine(to_string(pt));
			else
				currentFrame.SamplePoints.Add(pt);
		}



		public void AddPlane(ARKitPlane planeInfo)
		{
			if (ActiveMode == StreamMode.WriteToDiskStream) {
				disk_writer.WriteLine("pa " + to_string(planeInfo));
			} else {
				currentFrame.AddedPlanes.Add(planeInfo);
			}
		}
		public void UpdatePlane(ARKitPlane planeInfo)
		{
			if (ActiveMode == StreamMode.WriteToDiskStream) {
				disk_writer.WriteLine("pu " + to_string(planeInfo));
			} else {
				currentFrame.UpdatedPlanes.Add(planeInfo);
			}
		}
		public void RemovePlane(ARKitPlane planeInfo)
		{
			if (ActiveMode == StreamMode.WriteToDiskStream) {
				disk_writer.WriteLine("pr " + to_string(planeInfo));
			} else {
				currentFrame.RemovedPlanes.Add(planeInfo);
			}
		}



		string to_string(ARKitFrame f) {
			return string.Format("ff t {0} p {1} o {2}", f.realTime, f.camPos, f.camOrientation);
		}
		string to_string(ARKitPoint pt) {
			return string.Format("pt p {0} s {1} c {2}", pt.pos, pt.screenPos, pt.color);
		}


		string to_string(ARKitPlane p)
		{
			return string.Format("i {0} p {1} o {2} t {3} c {4} e {5} ", p.identifier, p.position, p.orientation,
								 (int)p.type, p.center, p.extents);
		}
        ARKitPlane plane_from_string(string[] tokens)
        {
            ARKitPlane p = new ARKitPlane();
            p.identifier = tokens[2];
            p.position = new Vector3(
                float.Parse(tokens[4]), float.Parse(tokens[5]), float.Parse(tokens[6]));
            p.orientation = new Quaternion(
                float.Parse(tokens[8]), float.Parse(tokens[9]), float.Parse(tokens[10]), float.Parse(tokens[11]));
            p.type = (PlaneType)int.Parse(tokens[13]);
            p.center = new Vector3(
                float.Parse(tokens[15]), float.Parse(tokens[16]), float.Parse(tokens[17]));
            p.extents = new Vector3(
                float.Parse(tokens[19]), float.Parse(tokens[20]), float.Parse(tokens[21]));
            return p;
        }





        void read_ascii(string sPath) {
			char[] sep = { ' ' };

			using ( StreamReader reader = new StreamReader(sPath) ) {
				while (!reader.EndOfStream) {
					string sLine = reader.ReadLine();
					string[] tokens = sLine.Split(sep);

					if ( tokens[0] == "ff" ) {
						float time = float.Parse(tokens[2]);
						Vector3 camPos = new Vector3(
							float.Parse(tokens[4]), float.Parse(tokens[5]), float.Parse(tokens[6]));
						Quaternion camOrientation = new Quaternion(
							float.Parse(tokens[8]), float.Parse(tokens[9]), float.Parse(tokens[10]), float.Parse(tokens[11]));
						BeginFrame(time, camPos, camOrientation);

					} else if ( tokens[0] == "pt" ) {
						Vector3 pos = new Vector3(
							float.Parse(tokens[2]), float.Parse(tokens[3]), float.Parse(tokens[4]));
						Vector2 screenPos = new Vector2(
							float.Parse(tokens[6]), float.Parse(tokens[7]));
						Color color = new Color(
							float.Parse(tokens[9]), float.Parse(tokens[10]), float.Parse(tokens[11]));
						AppendSample(pos, screenPos, color);

					} else if ( tokens[0] == "pa" || tokens[0] == "pu" || tokens[0] == "pr" ) {
                        ARKitPlane plane = plane_from_string(tokens);
                        if (tokens[0] == "pa")
                            AddPlane(plane);
                        else if (tokens[0] == "pu")
                            UpdatePlane(plane);
                        else
                            RemovePlane(plane);
                    }

				}
			}

		}


	}





}