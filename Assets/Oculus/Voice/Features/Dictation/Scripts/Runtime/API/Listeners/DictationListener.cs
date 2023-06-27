namespace Oculus.Voice.Dictation.Listeners
{
    public interface DictationListener
    {
        /// <summary>
        /// Called when dictation has started
        /// </summary>
        void OnStart(DictationListener listener);

        /// <summary>
        /// Called when mic level changes. Used for building UI.
        /// </summary>
        /// <param name="micLevel"></param>
        void OnMicAudioLevel(float micLevel);

        /// <summary>
        /// Called with current predicted transcription. Could change as user speaks.
        /// </summary>
        /// <param name="transcription"></param>
        void OnPartialTranscription(string transcription);

        /// <summary>
        /// Final transcription of what the user has said
        /// </summary>
        /// <param name="transcription"></param>
        void OnFinalTranscription(string transcription);

        /// <summary>
        /// Called when there was an error with the dictation service
        /// </summary>
        /// <param name="errorType">The type of error encountered</param>
        /// <param name="errorMessage">Human readable message describing the error</param>
        void OnError(string errorType, string errorMessage);

        /// <summary>
        /// Called when the dictation session is done
        /// </summary>
        void OnStopped(DictationListener listener);
    }
}
