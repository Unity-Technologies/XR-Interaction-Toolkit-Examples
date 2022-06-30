using System;
using UnityEngine;
using System.Collections;
using System.Text;

namespace DigitalOpus.MB.Core{
	// todo use the MeshBaker core version of this
	/// <summary>
	/// LOD stores a buffer of log messages for this object
	/// </summary>
	// 
	//
	/// <summary>
	/// LOD log.
	/// </summary> 
	public class LODLog{
		int pos = 0;
		string[] logMessages;
		
		public LODLog(short bufferSize){
			logMessages = new string[bufferSize];	
		}
		
		public void Log(MB2_LogLevel l, String msg, MB2_LogLevel currentThreshold){
			MB2_Log.Log(l,msg, currentThreshold);
			if (logMessages.Length == 0) return;
			if (l <= currentThreshold){
				logMessages[pos] = String.Format("frm={0} {1} {2}",Time.frameCount,l,msg);
				pos++;
				if (pos >= logMessages.Length) pos = 0;
			}
		}
		
		public string Dump(){
			StringBuilder sb = new StringBuilder();
			int startPos = 0;
			if (logMessages == null || logMessages.Length < 1) return "";
			if (logMessages[logMessages.Length - 1] != null) startPos = pos;
			for (int i = 0; i < logMessages.Length; i++){
				int ii = (startPos + i) % logMessages.Length;
				if (logMessages[ii] == null) break;
				sb.AppendLine(logMessages[ii]);
			}
			return sb.ToString();
		}
	}
}