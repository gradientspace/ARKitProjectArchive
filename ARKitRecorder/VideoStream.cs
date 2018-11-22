// Copyright (c) Ryan Schmidt - rms@gradientspace.com - twitter @rms80
// released under the MIT License (see LICENSE file)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace gs
{
	public class VideoFrame 
	{
		public float realTime;
		public int width;
		public int height;
		public byte[] rgb;
	}



	public class VideoStream
	{
		public List<VideoFrame> Frames;


		public enum StreamMode
		{
			WriteToDiskStream,
			ReadFromDiskStream,
			InMemoryStream
		};
		public StreamMode ActiveMode;

		BinaryWriter disk_writer;
		BinaryReader disk_reader;

		public void InitializeWrite(string sPath)
		{
			ActiveMode = StreamMode.WriteToDiskStream;
			disk_writer = new BinaryWriter(File.Open(sPath, FileMode.Create));

			writer_thread = new Thread(writer_thread_func);
			writer_thread.Start();
		}

		public void InitializeRead(string sPath, bool bReadAll = false)
		{
			if (bReadAll) {
				ActiveMode = StreamMode.InMemoryStream;
			} else {
				ActiveMode = StreamMode.ReadFromDiskStream;
			}
			disk_reader = new BinaryReader(File.Open(sPath, FileMode.Open));
			if (bReadAll)
				ReadAllFrames();
		}



		public void Shutdown()
		{
			if (disk_writer != null) {
				disk_writer.Close();
				disk_writer.BaseStream.Dispose();
			}
			if ( disk_reader != null ) {
				disk_reader.Close();
			}
			shutdown_thread = true;
		}



		public void AppendFrame(VideoFrame frame) {
			if (ActiveMode == StreamMode.WriteToDiskStream) {
				write_frame(frame);

			} else {
				Frames.Add(frame);
			}
		}



		public void ReadAllFrames() {
			Frames = new List<VideoFrame>();
			VideoFrame cur = null;
			do {
				cur = ReadNextFrame();
				if (cur != null)
					Frames.Add(cur);
			} while (cur != null);

			disk_reader.Close();
		}

		public VideoFrame ReadNextFrame() {
			if (disk_reader.BaseStream.Position == disk_reader.BaseStream.Length)
				return null;

			VideoFrame f = new VideoFrame();
			f.realTime = disk_reader.ReadSingle();
			f.width = disk_reader.ReadInt32();
			f.height = disk_reader.ReadInt32();

			int bytes = disk_reader.ReadInt32();
			f.rgb = disk_reader.ReadBytes(bytes);

			return f;
		}




		List<VideoFrame> WriteFrameQueue = new List<VideoFrame>();
		bool shutdown_thread = false;
		Thread writer_thread;

		void write_frame(VideoFrame frame) {
			lock(WriteFrameQueue) {
				WriteFrameQueue.Add(frame);
			}
		}


		void writer_thread_func() 
		{
			while (shutdown_thread == false || WriteFrameQueue.Count > 0) {

				VideoFrame frame = null;
				lock(WriteFrameQueue) {
					if ( WriteFrameQueue.Count > 0 ) {
						frame = WriteFrameQueue[0];
						WriteFrameQueue.RemoveAt(0);
					}
				}
				if (frame == null) {
					Thread.Sleep(100);
					continue;
				}
				actually_write_frame(frame);
			}
		}



		// todo: write in background thread...


		void actually_write_frame(VideoFrame frame) 
		{
			disk_writer.Write(frame.realTime);
			disk_writer.Write(frame.width);
			disk_writer.Write(frame.height);
			disk_writer.Write(frame.rgb.Length);
			disk_writer.Write(frame.rgb);
		}

	}


}