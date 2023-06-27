// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

namespace Oculus.Platform
{
  using System;
  using System.Collections.Generic;

  [Serializable]
  public class MetaGraphVariablesParam
  {
    public MetaGraphInputParam input;

    public MetaGraphVariablesParam(string client_mutation_id, string profile_type, string app_id) {
        input = new MetaGraphInputParam(client_mutation_id, profile_type, app_id);
    }
  }

  [Serializable]
  public class MetaGraphInputParam {
    public string client_mutation_id;
    public string profile_type;
    public string app_id;

    public MetaGraphInputParam(string client_mutation_id, string profile_type, string app_id) {
        this.client_mutation_id = client_mutation_id;
        this.profile_type = profile_type;
        this.app_id = app_id;
    }
  }
}
