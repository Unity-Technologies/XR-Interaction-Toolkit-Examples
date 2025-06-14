// Copyright (c) Meta Platforms, Inc. and affiliates.
// このファイルは提供されたプロジェクトの分析に基づき、デバッグ目的で作成されました。
using UnityEngine;
using NorthStar; // PhysicsTransformerが属する名前空間

/// <summary>
/// PhysicsTransformerの挙動をキーボード入力でデバッグするためのクラス。
/// ターゲットのPhysicsTransformerを直接呼び出し、実際のインタラクションに近い形で掴む/離す動作をシミュレートします。
/// このスクリプトは、仮想的な「手」となるGameObjectにアタッチしてください。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class KeyboardPhysicsGrabber : MonoBehaviour
{
    [Header("デバッグ対象")]
    [Tooltip("掴みたいオブジェクトにアタッチされているPhysicsTransformerコンポーネント")]
    public PhysicsTransformer targetTransformer;
    
    [SerializeField] KeyCode grabKey = KeyCode.G;
    
    [SerializeField] PhysicsRopeGrabAnchor anchor;

    // --- 内部変数 ---
    private Rigidbody m_handRigidbody;
    private bool m_isGrabbing = false;

    void Awake()
    {
        // このGameObject（仮想的な手）にRigidbodyがなければ追加
        if (!TryGetComponent(out m_handRigidbody))
        {
            m_handRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        // 手のRigidbodyは物理的な影響を受けず、Transformに追従するようKinematicに設定
        m_handRigidbody.isKinematic = true;
        m_handRigidbody.useGravity = false;

        this.anchor.Hand = this.transform;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (targetTransformer == null) return;

        // 'G'キーを押したらグラブ処理
        if (Input.GetKeyDown(this.grabKey))
        {
            if (!m_isGrabbing)
            {
                Grab();
            }
        }
        // 'G'キーを離したらリリース処理
        else if (Input.GetKeyUp(this.grabKey))
        {
            if (m_isGrabbing)
            {
                Release();
            }
        }
#endif
    }

    private void Grab()
    {
        m_isGrabbing = true;

        // PhysicsTransformerのAddInteractorを呼び出してグラブを開始します。
        // 第1引数: インタラクター（この手オブジェクト）
        // 第2引数: インタラクタブル（掴まれるオブジェクト）
        targetTransformer.AddInteractor(this.gameObject, targetTransformer.gameObject);

        Debug.Log($"'{targetTransformer.name}' を PhysicsTransformer を使ってグラブしました。");
    }

    private void Release()
    {
        m_isGrabbing = false;

        // PhysicsTransformerのRemoveInteractorを呼び出してグラブを終了します。
        targetTransformer.RemoveInteractor(this.gameObject);

        Debug.Log($"'{targetTransformer.name}' を解放しました。");
    }
}