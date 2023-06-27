using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#if !UNITY_EDITOR && UNITY_WEBGL
namespace VRBuilder.Core.IO
{
    public class WebGlFileSystem : DefaultFileSystem
    {
        public WebGlFileSystem(string streamingAssetsPath, string persistentDataPath) : base(streamingAssetsPath, persistentDataPath)
        {
        }

        protected override async Task<bool> FileExistsInStreamingAssets(string filePath)
        {
            string absolutePath = Path.Combine(StreamingAssetsPath, filePath);

            var webRequest = UnityWebRequest.Head(absolutePath);
            await webRequest.SendWebRequest();
            return webRequest.responseCode != 404;
        }

        protected override async Task<byte[]> ReadFromStreamingAssets(string filePath)
        {
            string absolutePath = Path.Combine(StreamingAssetsPath, filePath);

            var webRequest = UnityWebRequest.Get(absolutePath);
            await webRequest.SendWebRequest();
            return webRequest.downloadHandler.data;
        }
    }
}

public static class ExtensionMethods
{
    public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
    {
        TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
        if (asyncOp.isDone)
        {
            tcs.SetResult(null);
        }
        else
        {
            asyncOp.completed += obj => { tcs.SetResult(null); };
        }

        return ((Task)tcs.Task).GetAwaiter();
    }
}
#endif
