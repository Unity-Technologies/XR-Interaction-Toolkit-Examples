using System;
using UnityEngine;
using System.Collections;
using System.Text;

namespace DigitalOpus.MB.Core{
	 
	public enum MB2_LogLevel{
		none,
		error,
		warn,
		info,
		debug,
		trace
	}
	
	public class MB2_Log {
		
		public static void Log(MB2_LogLevel l, String msg, MB2_LogLevel currentThreshold){
			if (l <= currentThreshold) {
				if (l == MB2_LogLevel.error) Debug.LogError(msg);
				if (l == MB2_LogLevel.warn) Debug.LogWarning(String.Format("frm={0} WARN {1}",Time.frameCount,msg));
				if (l == MB2_LogLevel.info) Debug.Log(String.Format("frm={0} INFO {1}",Time.frameCount,msg));
				if (l == MB2_LogLevel.debug) Debug.Log(String.Format("frm={0} DEBUG {1}",Time.frameCount,msg));
				if (l == MB2_LogLevel.trace) Debug.Log(String.Format("frm={0} TRACE {1}",Time.frameCount,msg));				
			}	
		}

		public static string Error(string msg, params object[] args){
			string s = String.Format(msg, args);
			string s2 = String.Format("f={0} ERROR {1}", Time.frameCount,s);
			Debug.LogError(s2);
			return s2;
		}

		public static string Warn(string msg, params object[] args){
			string s = String.Format(msg, args);
			string s2 = String.Format("f={0} WARN {1}", Time.frameCount,s);
			Debug.LogWarning(s2);
			return s2;
		}		
		
		public static string Info(string msg, params object[] args){
			string s = String.Format(msg, args);
			string s2 = String.Format("f={0} INFO {1}", Time.frameCount,s);
			Debug.Log(s2);
			return s2;
		}
		
		public static string LogDebug(string msg, params object[] args){
			string s = String.Format(msg, args);
			string s2 = String.Format("f={0} DEBUG {1}", Time.frameCount,s);
			Debug.Log(s2);
			return s2;
		}
		
		public static string Trace(string msg, params object[] args){
			string s = String.Format(msg, args);
			string s2 = String.Format("f={0} TRACE {1}", Time.frameCount,s);
			Debug.Log(s2);
			return s2;
		}		
	}
	
	/// <summary>
	/// LOD stores a buffer of log messages specific to an object. These log messages are also written out to 
	/// the console.
	/// </summary>
	public class ObjectLog{
		int pos = 0;
		string[] logMessages;
		
		void _CacheLogMessage(string msg){
			if (logMessages.Length == 0) return;
			logMessages[pos] = msg;
			pos++;
			if (pos >= logMessages.Length) pos = 0;
		}
		
		public ObjectLog(short bufferSize){
			logMessages = new string[bufferSize];	
		}
		
		public void Log(MB2_LogLevel l, String msg, MB2_LogLevel currentThreshold){
			MB2_Log.Log(l,msg, currentThreshold);
			_CacheLogMessage(msg);
		}

		public void Error(string msg, params object[] args){
			_CacheLogMessage(MB2_Log.Error(msg,args));			
		}
		
		public void Warn(string msg, params object[] args){
			_CacheLogMessage(MB2_Log.Warn(msg,args));			
		}		
		
		public void Info(string msg, params object[] args){
			_CacheLogMessage(MB2_Log.Info(msg,args));			
		}
		
		public void LogDebug(string msg, params object[] args){
			_CacheLogMessage(MB2_Log.LogDebug(msg,args));		
		}
		
		public void Trace(string msg, params object[] args){
			_CacheLogMessage(MB2_Log.Trace(msg,args));		
		}
		
		public string Dump(){
			StringBuilder sb = new StringBuilder();
			int startPos = 0;
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