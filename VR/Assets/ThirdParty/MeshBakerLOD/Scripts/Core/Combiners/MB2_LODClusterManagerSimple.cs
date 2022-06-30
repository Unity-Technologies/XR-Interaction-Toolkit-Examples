//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Text;
using DigitalOpus.MB.Core;
using DigitalOpus.MB.Lod;

namespace DigitalOpus.MB.Lod{
	public class LODClusterSimple: LODClusterBase{
		public LODClusterSimple(LODClusterManagerSimple m):base(m){}		
		public override bool Contains(Vector3 v){return true;}
		public override bool Intersects(Bounds b){return true;}
		public override bool Intersects(Plane[][] fustrum){return true;}
		public override bool IsVisible(){return true;}
		public override float DistSquaredToPlayer(){return 0f;}
		public override Vector3 Center(){return Vector3.zero;}
		public override void DrawGizmos(){}
		public override void UpdateSkinnedMeshApproximateBounds(){
			for (int i = 0; i < combinedMeshes.Count; i++){
				combinedMeshes[i].UpdateSkinnedMeshApproximateBounds();
			}
		}
	}	
	
	/// <summary>
	/// M b2_ LOD cluster manager simple. All LOD objects are added to a single global cluster.
	/// </summary>
	public class LODClusterManagerSimple: LODClusterManager {		
		public LODClusterManagerSimple(MB2_LODManager.BakerPrototype bp):base(bp){
			clusters.Add(new LODClusterSimple(this));
		}
						
		public override LODCluster GetClusterFor(Vector3 p){ return clusters[0];}
				
		/// <summary>
		/// Does nothing. Does not remove the cluster for simple clustering scheme
		/// </summary>
		public override void RemoveCluster(Bounds c){
			Debug.LogWarning("Cannot remove clusters from ClusterManagerSimple");				
		}
		
		public override void DrawGizmos(){}
	}
}