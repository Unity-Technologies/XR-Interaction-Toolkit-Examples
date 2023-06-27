/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Meta.WitAi
{
    public static class WitConstants
    {
        // Wit service version info
        public const string API_VERSION = "20230313";
        public const string SDK_VERSION = "54.0.0";
        public const string CLIENT_NAME = "wit-unity";

        // Wit service endpoint info
        public const string URI_SCHEME = "https";
        public const string URI_AUTHORITY = "api.wit.ai";
        public const int URI_DEFAULT_PORT = -1;
        // Wit service header keys
        public const string HEADER_REQUEST_ID = "X-Wit-Client-Request-Id";
        public const string HEADER_AUTH = "Authorization";
        public const string HEADER_USERAGENT = "User-Agent";
        public const string HEADER_USERAGENT_PREFIX = "voice-sdk-54.0.0.135.284,";
        public const string HEADER_USERAGENT_CONFID_MISSING = "not-yet-configured";
        public const string HEADER_POST_CONTENT = "Content-Type";
        public const string HEADER_GET_CONTENT = "Accept";

        // NLP Endpoints
        public const string ENDPOINT_SPEECH = "speech";
        public const string ENDPOINT_MESSAGE = "message";
        public const string ENDPOINT_MESSAGE_PARAM = "q";
        public const string ENDPOINT_JSON_DELIMITER = "\r\n";
        public const string ENDPOINT_ERROR_PARAM = "error";

        // TTS Endpoint
        public const string ENDPOINT_TTS = "synthesize";
        public const string ENDPOINT_TTS_PARAM = "q";
        public const string ENDPOINT_TTS_CLIP = "WitTTSClip";
        public const string ENDPOINT_TTS_NO_TEXT = "No text provided";
        public const int ENDPOINT_TTS_CHANNELS = 1;
        public const int ENDPOINT_TTS_SAMPLE_RATE = 24000;
        public const int ENDPOINT_TTS_STREAM_CLIP_BUFFER = 5; // In Seconds
        public const float ENDPOINT_TTS_STREAM_READY_DURATION = 0.1f; // In Seconds
        public const int ENDPOINT_TTS_TIMEOUT = 10000; // In ms
        public const int ENDPOINT_TTS_MAX_TEXT_LENGTH = 280;

        // Dictation Endpoint
        public const string ENDPOINT_DICTATION = "dictation";

        // Composer Endpoints
        public const string ENDPOINT_COMPOSER_SPEECH = "converse";
        public const string ENDPOINT_COMPOSER_MESSAGE = "event";

        // Reusable constants
        public const string CANCEL_ERROR = "Cancelled";
        public const string CANCEL_MESSAGE_DEFAULT = "Request was cancelled.";
        public const string CANCEL_MESSAGE_PRE_SEND = "Request cancelled prior to transmission";

        /// <summary>
        /// Error code thrown when an exception is caught during processing or
        /// some other general error happens that is not an error from the server
        /// </summary>
        public const int ERROR_CODE_GENERAL = -1;
        /// <summary>
        /// Error code returned when no configuration is defined
        /// </summary>
        public const int ERROR_CODE_NO_CONFIGURATION = -2;
        /// <summary>
        /// Error code returned when the client token has not been set in the
        /// Wit configuration.
        /// </summary>
        public const int ERROR_CODE_NO_CLIENT_TOKEN = -3;
        /// <summary>
        /// No data was returned from the server.
        /// </summary>
        public const int ERROR_CODE_NO_DATA_FROM_SERVER = -4;
        /// <summary>
        /// Invalid data was returned from the server.
        /// </summary>
        public const int ERROR_CODE_INVALID_DATA_FROM_SERVER = -5;
        /// <summary>
        /// Request was aborted
        /// </summary>
        public const int ERROR_CODE_ABORTED = -6;
        /// <summary>
        /// Request to the server timeed out
        /// </summary>
        public const int ERROR_CODE_TIMEOUT = -7;
    }
}
