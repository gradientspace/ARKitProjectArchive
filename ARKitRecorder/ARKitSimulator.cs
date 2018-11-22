// Copyright (c) Ryan Schmidt - rms@gradientspace.com - twitter @rms80
// released under the MIT License (see LICENSE file)
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace gs
{

	public class ARKitSimulator : MonoBehaviour
	{
		// [RMS] Make it possible to easily look up this object from others. Kind of gross...
		public static ARKitSimulator ActiveSimulator = null;


		// stream that is currently playing
		ARKitStreamPlayback FakeStream = null;

		string current_filename = null;

		public bool ShowSamplePoints = true;
        public Material PlaneMaterial;
		GameObject samples_parent;
        GameObject planes_parent;


		// if you don't set this, video will not be played back
		public GameObject VideoPlane;


		//Use this for initialization
		void Start()
		{
			if (ActiveSimulator != null)
				throw new System.Exception("ARKitSimulator.Start: only one instance of this script can exist in scene!");
			ActiveSimulator = this;
		}

		// Update is called once per frame
		void Update()
		{
			if (FakeStream != null)
				FakeStream.Update(Time.realtimeSinceStartup);
		}



		public void Test() {
			string sFilename = Path.Combine(Application.dataPath,
				"../Recordings/ARStream_16-7-2017-22-25-2.txt");
			
			StartPlayback(sFilename);
		}



		public void StartPlayback(string sFilename)
		{
			if (FakeStream != null)
				FakeStream.Stop(true);
			
			if (current_filename != sFilename) {
				gs.ARKitStream stream = new gs.ARKitStream();
				stream.InitializeFromFileAscii(sFilename);

				string sVideoFilename = sFilename.Replace("ARStream", "VideoStream");
				sVideoFilename = sVideoFilename.Replace(".txt", ".bin");
				gs.VideoStream video_stream = null;
				if (File.Exists(sVideoFilename)) {
					video_stream = new gs.VideoStream();
					video_stream.InitializeRead(sVideoFilename, true);
				}

				FakeStream = new gs.ARKitStreamPlayback();
				FakeStream.SetSource(stream, video_stream);

				current_filename = sFilename;
			}


			samples_parent = new GameObject("samples_parent");
            planes_parent = new GameObject("planes_parent");

			Material videoMaterial = null;
			if (VideoPlane != null)
				videoMaterial = VideoPlane.GetComponent<Renderer>().material;

			Texture2D video_texture = null;

			FakeStream.NewFrameF = (time) => {
				reset_all_markers();
			};

			FakeStream.UpdateCameraF = (pos, rot) => {
				Camera.main.transform.position = pos;
				Camera.main.transform.rotation = rot;
			};

			FakeStream.EmitSamplePointF = (pos, screenPos, color) => {
				if (ShowSamplePoints) {
					GameObject sample_go = request_marker_go();
					sample_go.transform.position = pos;
					sample_go.GetComponent<MeshRenderer>().material.color = color;
				}
			};

            FakeStream.PlaneChangeF = (ARKitStream.PlaneChange eType, ARKitStream.ARKitPlane plane) => {
                if (eType == ARKitStream.PlaneChange.Removed)
                    remove_plane(plane);
                else
                    update_plane(plane);
            };

			FakeStream.EmitVideoFrameF = (frame) => {
				if (videoMaterial != null) {
					if (video_texture == null) {
						video_texture = new Texture2D(frame.width, frame.height);
					}
					video_texture.LoadImage(frame.rgb);
					videoMaterial.SetTexture("_MainTex", video_texture);
				}
			};

			FakeStream.Start(Time.realtimeSinceStartup);
		}






		List<GameObject> marker_pool = new List<GameObject>();
		int marker_pool_allocated = 0;

		void reset_all_markers() {
			for (int i = 0; i < marker_pool_allocated; ++i)
				marker_pool[i].SetActive(false);
			marker_pool_allocated = 0;
		}

		GameObject request_marker_go() {
			GameObject go;
			if ( marker_pool.Count == marker_pool_allocated ) {
				go = make_new_marker_go();
				marker_pool.Add(go);
			} else {
				go = marker_pool[marker_pool_allocated];
				go.SetActive(true);
			}
			marker_pool_allocated++;
			return go;
		}

		GameObject make_new_marker_go() {
			GameObject sphere_go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere_go.name = "sample_pt";
			sphere_go.transform.localScale = 0.01f * Vector3.one;
			sphere_go.transform.SetParent(samples_parent.transform, true);
			return sphere_go;
		}





        Dictionary<string, GameObject> planes = new Dictionary<string, GameObject>();

        void update_plane(ARKitStream.ARKitPlane plane)
        {
            if ( planes.ContainsKey(plane.identifier) == false ) {
                GameObject plane_go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane_go.name = "plane-" + plane.identifier;
                planes[plane.identifier] = plane_go;
                plane_go.transform.SetParent(planes_parent.transform, true);
                plane_go.transform.localScale = 0.1f * Vector3.one;

                if (PlaneMaterial != null)
                    plane_go.GetComponent<Renderer>().material = PlaneMaterial;
            }

            GameObject go = planes[plane.identifier];
            go.transform.position = plane.position;
            go.transform.rotation = plane.orientation;
            go.transform.position += plane.orientation * plane.center;
            go.transform.localScale = new Vector3(0.1f * plane.extents.x, 1, 0.1f * plane.extents.z);
        }


        void remove_plane(ARKitStream.ARKitPlane plane)
        {
            if ( planes.ContainsKey(plane.identifier) ) {
                GameObject go = planes[plane.identifier];
                GameObject.Destroy(go);
                planes.Remove(plane.identifier);
            }
        }

	}


}
