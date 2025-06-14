using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace NorthStar.XRInteractionHub
{
    
    [RequireComponent(typeof(Rigidbody))]
    public class SyncXRInteractor:MonoBehaviour
    {
        
        public XRBaseInteractor SyncGroup => syncGroup;
        
        [SerializeField] private XRBaseInteractor syncGroup;
        
        [SerializeField] PhysicsRopeGrabAnchor anchor;
        
        [SerializeField] PhysicsTransformer physicsTransformer;


        private void Awake()
        {
            this.anchor.Hand = this.transform;
        }

        private IEnumerator Start()
        {
            this.syncGroup.selectEntered.AddListener((
                arg0 =>
                {
                    Grab();
                }));
            
            this.syncGroup.selectExited.AddListener((
                arg0 =>
                {
                    Release();
                }));
            var rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            
            while (true)
            {
                rigidbody.transform.SetPositionAndRotation(syncGroup.transform.position, syncGroup.transform.rotation);
                yield return null;
            }
        }
        
        private void Grab()
        {
            // PhysicsTransformerのAddInteractorを呼び出してグラブを開始します。
            // 第1引数: インタラクター（この手オブジェクト）
            // 第2引数: インタラクタブル（掴まれるオブジェクト）
            physicsTransformer.AddInteractor(this.gameObject, physicsTransformer.gameObject);

            Debug.Log($"'{physicsTransformer.name}' を PhysicsTransformer を使ってグラブしました。");
        }

        private void Release()
        {
            // PhysicsTransformerのRemoveInteractorを呼び出してグラブを終了します。
            physicsTransformer.RemoveInteractor(this.gameObject);

            Debug.Log($"'{physicsTransformer.name}' を解放しました。");
        }
    }
}