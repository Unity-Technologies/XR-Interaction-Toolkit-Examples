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

namespace DigitalOpus.MB.Lod{
	
	public class LODClusterGrid: LODClusterBase{
		public Bounds b;
		public bool isVisible=false;
		public float distSquaredToPlayer = Mathf.Infinity;
		public int lastPrePrioritizeFrame = -1;
		public LODClusterGrid(Bounds b, LODClusterManagerGrid m):base(m){
			this.b = b;
		}
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
		public override string ToString(){
			return "LODClusterGrid " + b.ToString();
		}		
		public override void UpdateSkinnedMeshApproximateBounds(){
			Debug.LogError("Grid clusters cannot be used for skinned meshes");	
		}

		public void _TranslateCluster(Vector3 translation){
			b.center += translation;
			for (int i = 0; i < combinedMeshes.Count; i++){
				LODCombinedMesh cm = combinedMeshes[i];
				cm._TranslateLODs(translation);
			}
		}
	}
	
	public class LODClusterManagerGrid:LODClusterManager{
		public int _gridSize = 250;
		public LODClusterManagerGrid(MB2_LODManager.BakerPrototype bp):base(bp){ }
		public int gridSize{
			set {
				if (clusters.Count > 0) {
					MB2_Log.Log(MB2_LogLevel.error,"Can't change the gridSize once clusters exist.", LOG_LEVEL);
				} else {
					_gridSize = value;
				}
			}
			get {return _gridSize;}
		}

		//todo should be GetPostion and should be position of ancestor
		public override LODCluster GetClusterFor(Vector3 p){
			LODCluster cell = GetClusterContaining(p);
			if (cell == null){
				cell = new LODClusterGrid(new Bounds(new Vector3(_gridSize * Mathf.Round(p.x/_gridSize),
													_gridSize * Mathf.Round(p.y/_gridSize),
													_gridSize * Mathf.Round(p.z/_gridSize)),
													new Vector3(_gridSize,_gridSize,_gridSize)), this);
				if (MB2_LODManager.CHECK_INTEGRITY) cell.CheckIntegrity();
				clusters.Add(cell);
				if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.Log(MB2_LogLevel.debug,"Created new cell " + cell + " to contain point " + p,LOG_LEVEL);
			}			
			return cell;
		}

		public void TranslateAllClusters(Vector3 translation){
			for (int i = 0; i < clusters.Count; i++){
				LODClusterGrid cluster = (LODClusterGrid) clusters[i];
				cluster._TranslateCluster(translation);
			}
		}
	}


}