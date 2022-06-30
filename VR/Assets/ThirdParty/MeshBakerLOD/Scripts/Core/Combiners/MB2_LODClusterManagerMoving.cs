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

/*
LODClusterMoving was developed to fix a limitation with LODClusterSimple. The problem with LODClusterSimple,
is that meshes in each combined mesh can be distributed anywhere in the scene. This is bad because:
   1) most of these meshes could be out of the camera fustrum. This causes many unnecessary verts being sent to the GPU
   2) each combined mesh needs to be updated whenever one of its LODs changes. Because source meshes are 
      distributed randomly through the scene this leads to inefficeint baking.

Moving works like Simple except it has bounds which are updated when the cluster's LODs are checked. 
This should update the frequency with which LODs are checked so that the close units are checked more frequently
than distant units.

You can use LODClusterMoving if you know how your meshes will be grouped. For example you have soldier meshes 
organized into Units whos members stay near each other. You would create a separate baker for each unit,

How to use it:

1) Create a separate baker for each unit (Bakers can be added and removed from the manager
   at runtime dynamically using MB2_LODManager.AddBaker and MB2_LODManager.RemoveBaker:
       a) set the cluster type to "moving"
       b) give the baker a unique label
	The GridSize determines how often units are checked. 
	Units that are within GridSize are checked "Num Frames Between Checks". 
	Units that are within 2*Gridsize are checked 2*"Num Frames Between Checks" etc...
		
2) Set the label on each MB2_LOD the unit

*/

namespace DigitalOpus.MB.Lod{
	public class LODClusterMoving: LODClusterBase{
		public Bounds b;
		public bool isVisible=false;
		public float distSquaredToPlayer = Mathf.Infinity;
		public int lastPrePrioritizeFrame = -1;
		
		public LODClusterMoving(LODClusterManagerMoving m):base(m){}		
		public override bool Contains(Vector3 v){return b.Contains(v);}
		public override bool Intersects(Bounds b){return b.Intersects(b);}
		public override bool Intersects(Plane[][] fustrum){
			for (int i = 0; i < fustrum.Length; i++){
				if (GeometryUtility.TestPlanesAABB(fustrum[i],b)){  
					return true;
				}
			}
			return false;
		}
		public override Vector3 Center(){return b.center;}
		public override bool IsVisible(){
			return isVisible;
		}
		public override float DistSquaredToPlayer(){
			return distSquaredToPlayer;	
		}
		public override void PrePrioritize(Plane[][] fustrum, Vector3[] cameraPositions){
			if (lastPrePrioritizeFrame == Time.frameCount) return;
			isVisible = false;
			distSquaredToPlayer = Mathf.Infinity;
			for (int i = 0; i < cameraPositions.Length; i++){
				float d = (cameraPositions[i] - b.center).sqrMagnitude;
				if (distSquaredToPlayer > d) distSquaredToPlayer = d;	
			}
			lastPrePrioritizeFrame = Time.frameCount;
		}
		public override void DrawGizmos(){
			Gizmos.DrawWireCube(b.center,b.size);
		}
		
		public virtual void UpdateBounds(){
			b = combinedMeshes[0].CalcBounds();
			for (int i = 1; i < combinedMeshes.Count; i++){
				b.Encapsulate(combinedMeshes[i].CalcBounds());
			}
		}
		
		public override void UpdateSkinnedMeshApproximateBounds(){
			for (int i = 0; i < combinedMeshes.Count; i++){
				combinedMeshes[i].UpdateSkinnedMeshApproximateBounds();
			}
		}
	}	
	
	/// <summary>
	/// M b2_ LOD cluster manager moving. All LOD objects are added to a single global cluster.
	/// </summary>
	public class LODClusterManagerMoving: LODClusterManager {		
		public LODClusterManagerMoving(MB2_LODManager.BakerPrototype bp):base(bp){
			clusters.Add(new LODClusterMoving(this));
		}
						
		public override LODCluster GetClusterFor(Vector3 p){ return clusters[0];}
				
		/// <summary>
		/// Does nothing. Does not remove the cluster for simple clustering scheme
		/// </summary>
		public override void RemoveCluster(Bounds c){
			Debug.LogWarning("Cannot remove clusters from ClusterManagerMoving");				
		}
		
		public virtual void UpdateBounds(){
			Debug.LogWarning("Updating bounds.");
			((LODClusterMoving) clusters[0]).UpdateBounds();		
		}
	}
}