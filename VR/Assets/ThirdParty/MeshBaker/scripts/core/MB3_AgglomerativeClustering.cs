using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

namespace DigitalOpus.MB.Core
{
    [Serializable]
    public class MB3_AgglomerativeClustering
    {

        public List<item_s> items = new List<item_s>();

        public ClusterNode[] clusters;

        public bool wasCanceled;

        [Serializable]
        public class ClusterNode
        {
            public item_s leaf;
            public ClusterNode cha;
            public ClusterNode chb;
            public int height; /* height of node from the bottom */
            public float distToMergedCentroid;
            public Vector3 centroid; /* centroid of this cluster */
            public int[] leafs; /* indexes of root clusters merged */
            public int idx; //index in clusters list
            public bool isUnclustered = true;

            public ClusterNode(item_s ii, int index)
            {
                leaf = ii;
                idx = index;
                leafs = new int[1];
                leafs[0] = index;
                centroid = ii.coord;
                height = 0;
            }

            public ClusterNode(ClusterNode a, ClusterNode b, int index, int h, float dist, ClusterNode[] clusters)
            {
                cha = a;
                chb = b;
                idx = index;
                leafs = new int[a.leafs.Length + b.leafs.Length];
                Array.Copy(a.leafs, leafs, a.leafs.Length);
                Array.Copy(b.leafs, 0, leafs, a.leafs.Length, b.leafs.Length);
                Vector3 c = Vector3.zero;
                for (int i = 0; i < leafs.Length; i++)
                {
                    c += clusters[leafs[i]].centroid;
                }
                centroid = c / leafs.Length;
                height = h;
                distToMergedCentroid = dist;
            }
        };


        [Serializable]
        public class item_s
        {
            public GameObject go;
            public Vector3 coord; /* coordinate of the input data point */
        };

        float euclidean_distance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        public bool agglomerate(ProgressUpdateCancelableDelegate progFunc)
        {
            wasCanceled = true;
            if (progFunc != null) wasCanceled = progFunc("Filling Priority Queue:", 0);
            if (items.Count <= 1)
            {
                clusters = new ClusterNode[0];
                return false;
                //yield break;
            }
            clusters = new ClusterNode[items.Count * 2 - 1];
            for (int i = 0; i < items.Count; i++)
            {
                clusters[i] = new ClusterNode(items[i], i);
            }

            int numClussters = items.Count;
            List<ClusterNode> unclustered = new List<ClusterNode>();
            for (int i = 0; i < numClussters; i++)
            {
                clusters[i].isUnclustered = true;
                unclustered.Add(clusters[i]);
            }

            int height = 0;
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            float largestDistInQ = 0;
            long usedMemory = GC.GetTotalMemory(false) / 1000000;
            PriorityQueue < float, ClusterDistance > pq = new PriorityQueue<float, ClusterDistance>();
            //largestDistInQ = _RefillPriorityQWithSome(pq, unclustered, clusters /*,null,null*/);
            int numRefills = 0;
            while (unclustered.Count > 1)
            {
                
                int numToFindClosetPair = 0;
                height++;
                //find closest pair
                
                if (pq.Count == 0)
                {
                    numRefills++;
                    usedMemory = GC.GetTotalMemory(false) / 1000000;
                    if (progFunc != null) wasCanceled = progFunc("Refilling Q:" + ((float)(items.Count - unclustered.Count) * 100) / items.Count + " unclustered:" + unclustered.Count + " inQ:" + pq.Count + " usedMem:" + usedMemory,
                        ((float)(items.Count - unclustered.Count)) / items.Count);
                    largestDistInQ = _RefillPriorityQWithSome(pq, unclustered, clusters, progFunc);
                    if (pq.Count == 0) break;
                }
                KeyValuePair<float, ClusterDistance> closestPair = pq.Dequeue();
                // should only consider unclustered pairs. It is more effecient to discard nodes that have already been clustered as they are popped off the Q
                // than to try to remove them from the Q when they have been clustered.
                while (!closestPair.Value.a.isUnclustered || !closestPair.Value.b.isUnclustered) {
                    if (pq.Count == 0)
                    {
                        numRefills++;
                        usedMemory = GC.GetTotalMemory(false) / 1000000;
                        if (progFunc != null) wasCanceled = progFunc("Creating clusters:" + ((float)(items.Count - unclustered.Count) * 100) / items.Count + " unclustered:" + unclustered.Count + " inQ:" + pq.Count + " usedMem:" + usedMemory,
                            ((float)(items.Count - unclustered.Count)) / items.Count);
                        largestDistInQ = _RefillPriorityQWithSome(pq, unclustered, clusters, progFunc);
                        if (pq.Count == 0) break;
                    }
                    closestPair = pq.Dequeue();
                    numToFindClosetPair++;
                }

                //make a new cluster with pair as children set merge height
                numClussters++;
                ClusterNode cn = new ClusterNode(closestPair.Value.a, closestPair.Value.b, numClussters - 1, height, closestPair.Key, clusters);
                //remove children from unclustered
                unclustered.Remove(closestPair.Value.a);
                unclustered.Remove(closestPair.Value.b);


                //We NEED TO REMOVE ALL DISTANCE PAIRS THAT INVOLVE A AND B FROM PRIORITY Q. However searching for all these pairs and removing is very slow.
                // Instead we will leave them in the Queue and flag the clusters as isUnclustered = false and discard them as they are popped from the Q which is O(1) operation.
                closestPair.Value.a.isUnclustered = false;
                closestPair.Value.b.isUnclustered = false;

                //add new cluster to unclustered
                int newIdx = numClussters - 1;
                if (newIdx == clusters.Length)
                {
                    Debug.LogError("how did this happen");
                }
                clusters[newIdx] = cn;
                unclustered.Add(cn);
                cn.isUnclustered = true;
                //update new clusteres distance
                for (int i = 0; i < unclustered.Count - 1; i++)
                {

                    float dist = euclidean_distance(cn.centroid, unclustered[i].centroid);
                    if (dist < largestDistInQ) //avoid cluttering Qwith
                    {
                        pq.Add(new KeyValuePair<float, ClusterDistance>(dist, new ClusterDistance(cn, unclustered[i])));
                    }
                }
                //if (timer.Interval > .2f)
                //{
                //    yield return null;
                //    timer.Start();
                //}
                if (wasCanceled) break;
                usedMemory = GC.GetTotalMemory(false) / 1000000;
                if (progFunc != null) wasCanceled = progFunc("Creating clusters:" + ((float)(items.Count - unclustered.Count)*100) / items.Count + " unclustered:" + unclustered.Count + " inQ:" + pq.Count + " usedMem:" + usedMemory, 
                    ((float)(items.Count - unclustered.Count)) / items.Count);
            }
            if (progFunc != null) wasCanceled = progFunc("Finished clustering:", 100);
            //Debug.Log("Time " + timer.Elapsed);
            if (wasCanceled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        const int MAX_PRIORITY_Q_SIZE = 2048;
        float _RefillPriorityQWithSome(PriorityQueue<float, ClusterDistance> pq, List<ClusterNode> unclustered, ClusterNode[] clusters, ProgressUpdateCancelableDelegate progFunc)
        {
            //find nthSmallest point of distances between pairs
            List<float> allDist = new List<float>(2048);
            for (int i = 0; i < unclustered.Count; i++)
            {
                for (int j = i+1; j < unclustered.Count; j++)
                {
                    
                   // if (unclustered[i] == omitA || unclustered[i] == omitB ||
                   //     unclustered[j] == omitA || unclustered[j] == omitB)
                   // {
                        
                   // } else
                   // {
               
                        allDist.Add(euclidean_distance(unclustered[i].centroid, unclustered[j].centroid));
                   // }
                }
                wasCanceled = progFunc("Refilling Queue Part A:", i / (unclustered.Count * 2f));
                if (wasCanceled) return 10f;
            }
            
            if (allDist.Count == 0)
            {
                return 10e10f;
            }
            float nthSmallest = NthSmallestElement(allDist, MAX_PRIORITY_Q_SIZE);

            //load up Q with up to nthSmallest distance pairs
            for (int i = 0; i < unclustered.Count; i++)
            {
                for (int j = i + 1; j < unclustered.Count; j++)
                {
                    int idxa = unclustered[i].idx;
                    int idxb = unclustered[j].idx;
                    float newDist = euclidean_distance(unclustered[i].centroid, unclustered[j].centroid);
                    if (newDist <= nthSmallest)
                    {
                        pq.Add(new KeyValuePair<float, ClusterDistance>(newDist, new ClusterDistance(clusters[idxa], clusters[idxb])));
                    }
                }
                wasCanceled = progFunc("Refilling Queue Part B:", (unclustered.Count + i) / (unclustered.Count * 2f));
                if (wasCanceled) return 10f;
            }
            return nthSmallest;
        }

        public int TestRun(List<GameObject> gos)
        {
            List<item_s> its = new List<item_s>();
            for (int i = 0; i < gos.Count; i++)
            {
                item_s ii = new item_s();
                ii.go = gos[i];
                ii.coord = gos[i].transform.position;
                its.Add(ii);
            }
            items = its;
            if (items.Count > 0)
            {
                agglomerate(null);
            }
            return 0;
        }


        //------
        //    Unclustered
        //need to be able to find the smallest distance between unclustered pairs quickly
        //Do this by maintaining a fixed length PriorityQueue (len = 1000)
        //   Q stores min distances between cluster pairs
        //   unlclustered stores list of unclustered
        //GetMin
        //    if Q is empty
        //          build Q from unclustered O(n2)
        //          track the largestDistanceInQ
        //          if unclustered is empty we are done
        //    else
        //          q.DeQueue O(1)
        //
        //    when creating new merged cluster, calc dist to all other unclustered add these distances to priority Q if less than largestDistanceInQ O(N)
        //

        public class ClusterDistance
        {
            public ClusterNode a;
            public ClusterNode b;
            public ClusterDistance(ClusterNode aa, ClusterNode bb)
            {
                a = aa;
                b = bb;
            }
        }



            public static void Main()
            {

                List<float> inputArray = new List<float>();
                inputArray.AddRange(new float[] { 19, 18, 17, 16, 15, 10, 11, 12, 13, 14 });
                // Loop 10 times
                Debug.Log("Loop quick select 10 times.");

                Debug.Log(NthSmallestElement(inputArray, 0));
                
            }

            // n is 0 indexed
            public static T NthSmallestElement<T>(List<T> array, int n) where T : IComparable<T>
            {
                if (n < 0)
                    n = 0;

                if (n > array.Count - 1)
                    n = array.Count - 1;
                if (array.Count == 0)
                    throw new ArgumentException("Array is empty.", "array");
                if (array.Count == 1)
                    return array[0];

                return QuickSelectSmallest(array, n)[n];
            }

            private static List<T> QuickSelectSmallest<T>(List<T> input, int n) where T : IComparable<T>
            {
                // Let's not mess up with our input array
                // For very large arrays - we should optimize this somehow - or just mess up with our input
                var partiallySortedArray = input;

                // Initially we are going to execute quick select to entire array
                var startIndex = 0;
                var endIndex = input.Count - 1;

                // Selecting initial pivot
                // Maybe we are lucky and array is sorted initially?
                var pivotIndex = n;

                // Loop until there is nothing to loop (this actually shouldn't happen - we should find our value before we run out of values)
                var r = new System.Random();
                while (endIndex > startIndex)
                {
                    pivotIndex = QuickSelectPartition(partiallySortedArray, startIndex, endIndex, pivotIndex);
                    if (pivotIndex == n)
                        // We found our n:th smallest value - it is stored to pivot index
                        break;
                    if (pivotIndex > n)
                        // Array before our pivot index have more elements that we are looking for                    
                        endIndex = pivotIndex - 1;
                    else
                        // Array before our pivot index has less elements that we are looking for                    
                        startIndex = pivotIndex + 1;

                    // Omnipotent beings don't need to roll dices - but we do...
                    // Randomly select a new pivot index between end and start indexes (there are other methods, this is just most brutal and simplest)
                    pivotIndex = r.Next(startIndex, endIndex);
                }
                return partiallySortedArray;
            }

            private static int QuickSelectPartition<T>(List<T> array, int startIndex, int endIndex, int pivotIndex) where T : IComparable<T>
            {
                var pivotValue = array[pivotIndex];
                // Initially we just assume that value in pivot index is largest - so we move it to end (makes also for loop more straight forward)
                Swap(array, pivotIndex, endIndex);
                for (var i = startIndex; i < endIndex; i++)
                {
                    if (array[i].CompareTo(pivotValue) > 0)
                        continue;

                    // Value stored to i was smaller than or equal with pivot value - let's move it to start
                    Swap(array, i, startIndex);
                    // Move start one index forward 
                    startIndex++;
                }
                // Start index is now pointing to index where we should store our pivot value from end of array
                Swap(array, endIndex, startIndex);
                return startIndex;
            }

            private static void Swap<T>(List<T> array, int index1, int index2)
            {
                if (index1 == index2)
                    return;

                var temp = array[index1];
                array[index1] = array[index2];
                array[index2] = temp;
            }
       
    }
}
