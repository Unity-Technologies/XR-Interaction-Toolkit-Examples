// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

namespace Oculus.Platform
{
  using System;
  using System.Collections.Generic;

  [Serializable]
  public class HorizonProfileTokenResponse
  {
    public DataModel data;
  }

  [Serializable]
  public class DataModel {
    public XfrCreateProfileTokenModel xfr_create_profile_token;
  }

  [Serializable]
  public class XfrCreateProfileTokenModel {
    public List<ProfileTokenModel> profile_tokens;
  }

  [Serializable]
  public class ProfileTokenModel {
    public string profile_id;
    public string access_token;
  }
}
