using System;
using System.Collections;
using System.Collections.Generic;
using Meta.Conduit;
using Meta.WitAi;
using UnityEngine;

namespace Oculus.Voice.Demo.ConduitChessDemo
{
    public class ChessBoardController : MonoBehaviour
    {

        public GameObject letters;
        public GameObject numbers;
        public GameObject chessPiece;
        public UnityEngine.UI.Text errorText;
        private Vector3 _targetPosition = new Vector3(0,2,0);


        // Update is called once per frame
        void Update()
        {
            chessPiece.transform.position = Vector3.Lerp(chessPiece.transform.position, _targetPosition, Time.deltaTime);
        }

        public enum ChessBoardLetter
        {
            A,
            B,
            C,
            D,
            E,
            F,
            G,
            H
        }
        [MatchIntent("MoveChessPiece")]
        public void MoveChessPiece(ChessBoardLetter letter, int number)
        {
            Debug.Log("Move chess piece to " + letter + number);

            _targetPosition = new Vector3(letters.transform.GetChild((int)letter).position.x, _targetPosition.y,
                numbers.transform.GetChild(number - 1).position.z);

        }

        [HandleEntityResolutionFailure]
        public void OnHandleEntityResolutionFailure(string intent , Exception ex)
        {
            Debug.Log("Failed to resolve parameter for intent " + intent + " with error " + ex.Message);
            errorText.text = ex.Message;
        }

    }
}
