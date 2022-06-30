/**
 *	\brief Hax!  DLLs cannot interpret preprocessor directives, so this class acts as a "bridge"
 */
using System;
using UnityEngine;
using System.Collections;

namespace DigitalOpus.MB.Core{
	//todo use the mesh baker core version of this
	public class MB2_Version
	{
		public static int GetMajorVersion(){
	#if UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5	
			return 3;
	#else
			return 4;
	#endif	
		}
	
		public static bool GetActive(GameObject go){
	#if UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5	
			return go.active;
	#else
			return go.activeInHierarchy;
	#endif			
		}
	
		public static void SetActive(GameObject go, bool isActive){
	#if UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5	
			go.active = isActive;
	#else
			go.SetActive(isActive);
	#endif
		}
		
		public static void SetActiveRecursively(GameObject go, bool isActive){
	#if UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5	
			go.SetActiveRecursively(isActive);
	#else
			go.SetActive(isActive);
	#endif
		}
		
		public static UnityEngine.Object[] FindSceneObjectsOfType(Type t){
	#if UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5	
			return GameObject.FindSceneObjectsOfType(t);
	#else
			return GameObject.FindObjectsOfType(t);
	#endif				
		}
	}
}