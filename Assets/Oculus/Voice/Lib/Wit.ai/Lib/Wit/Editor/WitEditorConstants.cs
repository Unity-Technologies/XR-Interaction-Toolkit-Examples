/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Meta.WitAi
{
    public static class WitEditorConstants
    {
        // Apps Endpoint
        public const string ENDPOINT_APPS = "apps";
        public const string ENDPOINT_APPS_LIMIT = "limit";
        public const string ENDPOINT_APPS_OFFSET = "offset";
        public const string ENDPOINT_APP_FOR_TOKEN = "is_app_for_token";
        public const string ENDPOINT_APP_ID = "id";

        // Info Endpoints
        public const string ENDPOINT_CLIENTTOKENS = "client_tokens";
        public const string ENDPOINT_CLIENTTOKENS_VAL = "client_token";
        public const string ENDPOINT_INTENTS = "intents";
        public const string ENDPOINT_ENTITIES = "entities";
        public const string ENDPOINT_TRAITS = "traits";
        public const string ENDPOINT_TTS_VOICES = "voices";

        // Add endpoints
        public const string ENDPOINT_IMPORT = "import";
        public const string ENDPOINT_ADD_INTENT = "intents";
        public const string ENDPOINT_ADD_ENTITY = "entities";
        public const string ENDPOINT_ADD_ENTITY_KEYWORD = "keywords";
        public const string ENDPOINT_ADD_ENTITY_KEYWORD_SYNONYMS = "synonyms";
        public const string ENDPOINT_ADD_TRAIT = "traits";
        public const string ENDPOINT_ADD_TRAIT_VALUE = "values";

        // Export endpoints
        public const string ENDPOINT_EXPORT = "export";
    }
}
