using UnityEngine;
using System.Collections;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Text;
using DigitalOpus.MB.Core;
using DigitalOpus.MB.Lod;

namespace DigitalOpus.MB.Lod{

	public abstract class LODClusterManager {
		
		public MB2_LogLevel _LOG_LEVEL = MB2_LogLevel.info;
		
		public MB2_LogLevel LOG_LEVEL{
			get{ return _LOG_LEVEL;}
			set{ _LOG_LEVEL = value;}
		}
		
		public MB2_LODManager.BakerPrototype _bakerPrototype;
		public List<LODCluster> clusters = new List<LODCluster>();
		
		protected List<LODCombinedMesh> recycledClusters = new List<LODCombinedMesh>();
		
		public LODClusterManager(MB2_LODManager.BakerPrototype bp){
			_bakerPrototype = bp;	
		}
		
		public virtual MB2_LODManager.BakerPrototype GetBakerPrototype(){return _bakerPrototype;}
		
		public virtual void Destroy(){
			for (int i = clusters.Count -1; i >= 0; i--){
				clusters[i].Destroy();
			}
			clusters.Clear();
			recycledClusters.Clear();
		}
		
		public virtual LODCluster GetClusterContaining(Vector3 v){
			for (int i = 0; i < clusters.Count; i++){
				if (clusters[i].Contains(v)){
					return clusters[i];	
				}
			}
			return null;
		}	
		
		public virtual void RemoveCluster(Bounds b){
			LODCluster c = GetClusterIntersecting(b);
			if (c != null){
				c.Clear();
				clusters.Remove(c);
			}
		}

		public virtual void Clear(){
			for (int i = clusters.Count - 1; i >= 0; i--){
				clusters[i].Clear();
				clusters.RemoveAt(i);
			}
		}
		
		public virtual void RecycleCluster(LODCombinedMesh c){
			if (c == null) return;
			if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.Log(MB2_LogLevel.debug,"LODClusterManagerGrid.RecycleCluster",LOG_LEVEL);
			c.Clear();
			c.cluster = null;
			if (!recycledClusters.Contains(c)) recycledClusters.Add(c);
			if (c.combinedMesh.resultSceneObject != null) MB2_Version.SetActiveRecursively(c.combinedMesh.resultSceneObject,false);
		}
					
		public virtual void DrawGizmos(){
			for (int j = 0; j < clusters.Count; j++){
				clusters[j].DrawGizmos();
			}		
		}
		
		public virtual void CheckIntegrity(){
			for (int i = 0; i < clusters.Count; i++){
				clusters[i].CheckIntegrity();
			}
			for (int i = 0; i < recycledClusters.Count; i++){
				recycledClusters[i].CheckIntegrity();
			}		
		}
		
		public virtual LODCombinedMesh GetFreshCombiner(LODCluster cell){
			LODCombinedMesh c = null;
			if (recycledClusters.Count > 0){ 
				c = recycledClusters[recycledClusters.Count-1];
				recycledClusters.RemoveAt(recycledClusters.Count-1);
				c.SetLODCluster(cell);
			} else {
				c = new LODCombinedMesh(_bakerPrototype.meshBaker, cell);
			}
			if (c.combinedMesh.resultSceneObject != null) MB2_Version.SetActiveRecursively(c.combinedMesh.resultSceneObject,true);
			cell.AddCombiner(c);
			c.numFramesBetweenChecks = -1;
			c.numFramesBetweenChecksOffset = -1;
			if (c.combinedMesh != null && c.combinedMesh.resultSceneObject != null){
				c.combinedMesh.resultSceneObject.name = c.combinedMesh.resultSceneObject.name.Replace("-recycled","");
			}
			cell.nextCheckFrame = Time.frameCount + 1;
			return c;
		}
		
		public virtual void UpdateSkinnedMeshApproximateBounds(){
			for (int i = 0; i < clusters.Count; i++){
				clusters[i].UpdateSkinnedMeshApproximateBounds();
			}
		}
		
		public abstract LODCluster GetClusterFor(Vector3 p);
		
		public virtual void ForceCheckIfLODsChanged(){
			for (int i = 0; i < clusters.Count; i++){
				clusters[i].ForceCheckIfLODsChanged();
			}
		}
		
		LODCluster GetClusterIntersecting(Bounds b){
			for (int i = 0; i < clusters.Count; i++){
				if (clusters[i].Intersects(b)){
					return clusters[i];	
				}
			}
			return null;
		}		
	}	
	
	public class MB2_LODClusterComparer : IComparer<LODCombinedMesh>{
	   int IComparer<LODCombinedMesh>.Compare(LODCombinedMesh a, LODCombinedMesh b){
			//want to sort in reversed order.
			LODCombinedMesh aa = b;
			LODCombinedMesh bb = a;
			//compare bakeImmediately
			int numBakeImmdediateCompare = bb.NumBakeImmediately() - aa.NumBakeImmediately();
			if (numBakeImmdediateCompare != 0) return numBakeImmdediateCompare;
			
			//compare visibility
			int visCompare;
			if (aa.IsVisible() && !bb.IsVisible()){ 
				visCompare = -1;
			} else if (aa.IsVisible() == bb.IsVisible()){
				visCompare = 0;
			} else {
				visCompare = 1;
			}
			if (visCompare != 0) return visCompare;
			
			return bb.NumDirty() - aa.NumDirty();
			
//			//compare add delete
//			int numAddDeleteCompare = bb.NumDirtyAdd() + bb.NumDirtyRemove() - aa.NumDirtyAdd() - aa.NumDirtyRemove();
//			if (numAddDeleteCompare != 0) return numAddDeleteCompare;
//			
//			//compare update
//			int numUpdateCompare = bb.NumDirtyUpdate() - aa.NumDirtyUpdate();
//			return numUpdateCompare;
	   }
	}
	

}
