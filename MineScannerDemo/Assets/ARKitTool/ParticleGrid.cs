using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g3;

public class ParticleGrid
{
	struct PInfo {
		public int count;
		public Vector3f AccumColor;
	}
	Dictionary<Vector3i, PInfo> Particles;
	public float GridStepSize = 0.02f;

	public Vector3f Origin = Vector3f.Zero;
	public AxisAlignedBox3i Extents;


	public bool UniqueParticles = false;
	HashSet<Vector3f> Uniques;

	public ParticleGrid()
	{
		Particles = new Dictionary<Vector3i, PInfo>();
		Extents = AxisAlignedBox3i.Empty;

		Uniques = new HashSet<Vector3f>();
	}


	public void AddParticle(Vector3f v, Colorf c)
	{
		v -= Origin;

		if (UniqueParticles && Uniques.Contains(v))
			return;

		Vector3i key = ToIndex(v);
		PInfo pinfo;
		if (Particles.TryGetValue(key, out pinfo)) {
			pinfo.count++;
			//pinfo.AccumColor += new Vector3f(c.r, c.g, c.b);
			pinfo.AccumColor = pinfo.count * new Vector3f(c.r, c.g, c.b);
			Particles[key] = pinfo;
		} else {
			pinfo = new PInfo() { count = 1, AccumColor = c };
			Particles.Add(key, pinfo);
			Extents.Contain(key);
		}

		if ( UniqueParticles )
			Uniques.Add(v);
	}


	public Vector3i ToIndex(Vector3f v) {
		v -= Origin;
		v /= GridStepSize;
		int i = (int)(v.x + 0.5f) - ((v.x < -0.5f) ? 1 : 0);
		int j = (int)(v.y + 0.5f) - ((v.y < -0.5f) ? 1 : 0);
		int k = (int)(v.z + 0.5f) - ((v.z < -0.5f) ? 1 : 0);
		return new Vector3i(i, j, k);
	}

	public Vector3f ToXYZ(Vector3i idx) {
		float x = (float)idx.x * GridStepSize;
		float y = (float)idx.y * GridStepSize;
		float z = (float)idx.z * GridStepSize;
		Vector3f v = new Vector3f(x, y, z);
		v += Origin;
		return v;
	}

	public Colorf GetColor(Vector3i idx) {
		PInfo pinfo;
		if ( Particles.TryGetValue(idx, out pinfo) ) {
			Vector3f c = pinfo.AccumColor / (float)pinfo.count;
			return new Colorf(c.x, c.y, c.z);
		}
		return Colorf.White;
	}


	public int GridPointCount(int minSamples = 1) {
		int count = 0;
		foreach (var pair in Particles) {
			if (pair.Value.count >= minSamples)
				count++;
		}
		return count;
	}


	public IEnumerable<Vector3i> GridIndices(int minSamples = 1) {
		foreach (var pair in Particles) {
			if (pair.Value.count < minSamples)
				continue;
			yield return pair.Key;
		}
	}

	public IEnumerable<Vector3f> GridPoints(int minSamples = 1) {
		foreach ( var pair in Particles ) {
			if (pair.Value.count < minSamples)
				continue;
			yield return ToXYZ(pair.Key);
		}
	}


}
