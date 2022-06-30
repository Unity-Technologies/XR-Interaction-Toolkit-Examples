using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DigitalOpus.MB.Core{
    public class MB3_KMeansClustering {

        class DataPoint
        {
            public Vector3 center;
            public GameObject gameObject;
            public int Cluster;

            public DataPoint(GameObject go)
            {
                gameObject = go;
                center = go.transform.position;
                if (go.GetComponent<Renderer>() == null) Debug.LogError("Object does not have a renderer " + go);
            }
        }

        List<DataPoint> _normalizedDataToCluster = new List<DataPoint>();
        Vector3[] _clusters = new Vector3[0];
        private int _numberOfClusters = 0;

        public MB3_KMeansClustering(List<GameObject> gos, int numClusters)
        {
            for (int i = 0; i < gos.Count; i++)
            {
                if (gos[i] != null)
                {
                    DataPoint dp = new DataPoint(gos[i]);
                    _normalizedDataToCluster.Add(dp);
                } else
                {
                    Debug.LogWarning(String.Format("Object {0} in list of objects to cluster was null.", i));
                }
            }
            if (numClusters <= 0)
            {
                Debug.LogError("Number of clusters must be posititve.");
                numClusters = 1;
            }
            if (_normalizedDataToCluster.Count <= numClusters)
            {
                Debug.LogError("There must be fewer clusters than objects to cluster");
                numClusters = _normalizedDataToCluster.Count - 1;
            }
            _numberOfClusters = numClusters;
            if (_numberOfClusters <= 0) _numberOfClusters = 1;
            
            _clusters = new Vector3[_numberOfClusters];
        }

        private void InitializeCentroids()
        {
            //todo error if more clusters than objs
            for (int i = 0; i < _numberOfClusters; ++i)
            {
                _normalizedDataToCluster[i].Cluster =  i;
            }
            for (int i = _numberOfClusters; i < _normalizedDataToCluster.Count; i++)
            {
                _normalizedDataToCluster[i].Cluster = UnityEngine.Random.Range(0, _numberOfClusters);
            }
        }

        private bool UpdateDataPointMeans(bool force)
        {
            if (AnyAreEmpty(_normalizedDataToCluster) && !force) return false;
            Vector3[] means = new Vector3[_numberOfClusters];
            int[] numInCluster = new int[_numberOfClusters];

            for (int i = 0; i < _normalizedDataToCluster.Count; i++)
            {
                int idx = _normalizedDataToCluster[i].Cluster;
                means[idx] += _normalizedDataToCluster[i].center;
                numInCluster[idx]++;
            }
            for (int i = 0; i < _numberOfClusters; i++)
            {
                _clusters[i] = means[i] / numInCluster[i]; 
            }
            return true;
        }

        private bool AnyAreEmpty(List<DataPoint> data)
        {
            int[] numInCluster = new int[_numberOfClusters];
            for (int i = 0; i < _normalizedDataToCluster.Count; i++)
            {
                numInCluster[_normalizedDataToCluster[i].Cluster]++;
            }

            for (int i = 0; i < numInCluster.Length; i++)
            {
                if (numInCluster[i] == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private bool UpdateClusterMembership()
        {
            bool changed = false;

            float[] distances = new float[_numberOfClusters];

            for (int i = 0; i < _normalizedDataToCluster.Count; ++i)
            {

                for (int k = 0; k < _numberOfClusters; ++k)
                {
                    distances[k] = ElucidanDistance(_normalizedDataToCluster[i], _clusters[k]);
                }
                int newClusterId = MinIndex(distances);
                if (newClusterId != _normalizedDataToCluster[i].Cluster)
                {
                    changed = true;
                    _normalizedDataToCluster[i].Cluster = newClusterId;

                }
                else
                {
                
                }

            }
            if (changed == false) return false;
            //if (AnyAreEmpty(_normalizedDataToCluster)) return false;
            return true;
        }

        private float ElucidanDistance(DataPoint dataPoint, Vector3 mean)
        {
            return Vector3.Distance(dataPoint.center, mean);
        }

        private int MinIndex(float[] distances)
        {
            int _indexOfMin = 0;
            double _smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < _smallDist)
                {
                    _smallDist = distances[k];
                    _indexOfMin = k;
                }
            }
            return _indexOfMin;
        }

        public List<Renderer> GetCluster(int idx, out Vector3 mean, out float size)
        {
            if (idx < 0 || idx >= _numberOfClusters)
            {
                Debug.LogError("idx is out of bounds");
                mean = Vector3.zero;
                size = 1;
                return new List<Renderer>();
            }
            UpdateDataPointMeans(true);
            List<Renderer> gos = new List<Renderer>();
            mean = _clusters[idx];
            float longestDist = 0; 
            for (int i = 0; i < _normalizedDataToCluster.Count; i++)
            {
                if (_normalizedDataToCluster[i].Cluster == idx)
                {
                    float dist = Vector3.Distance(mean, _normalizedDataToCluster[i].center);
                    if (dist > longestDist) longestDist = dist;
                    gos.Add(_normalizedDataToCluster[i].gameObject.GetComponent<Renderer>());
                }
            }
            mean = _clusters[idx];
            size = longestDist; //todo should be greatest distance to mean
            return gos;
        }

        public void Cluster()
        {
            bool _changed = true;
            bool _success = true;
            InitializeCentroids();

            int maxIteration = _normalizedDataToCluster.Count * 1000;
            int _threshold = 0;
            while (_success == true && _changed == true && _threshold < maxIteration)
            {
                ++_threshold;
                _success = UpdateDataPointMeans(false);
                _changed = UpdateClusterMembership();
            }
        }
    }
}
