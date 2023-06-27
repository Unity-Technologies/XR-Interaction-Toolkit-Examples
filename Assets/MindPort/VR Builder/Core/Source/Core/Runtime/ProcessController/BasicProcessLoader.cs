using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Configuration;

namespace VRBuilder.ProcessController
{
    /// <summary>
    /// Loads and starts the process currently selected in the 'PROCESS_CONFIGURATION' gameObject.
    /// </summary>
    public class BasicProcessLoader : MonoBehaviour, IConfigurableProcessController
    {
        /// <inheritdoc />
        public bool AutoStartProcess { get; set; }

        private void Start()
        {
            StartCoroutine(StartProcess());
        }

        private IEnumerator StartProcess()
        {
            // Load process from a file.
            string processPath = RuntimeConfigurator.Instance.GetSelectedProcess();

            // Try to load the in the PROCESS_CONFIGURATION selected process.

            Task<IProcess> loadProcess = RuntimeConfigurator.Configuration.LoadProcess(processPath);
            while (!loadProcess.IsCompleted)
            {
                yield return null;
            }

            IProcess process = loadProcess.Result;

            // Initializes the process.
            ProcessRunner.Initialize(process);

            // Runs the process.
            if(AutoStartProcess)
            {
                ProcessRunner.Run();
            }
        }
    }
}
