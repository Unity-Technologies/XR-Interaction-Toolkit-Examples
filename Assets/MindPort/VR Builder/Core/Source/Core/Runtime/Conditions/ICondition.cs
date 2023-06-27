// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core.Conditions
{
    public interface ICondition: ICompletableEntity, IDataOwner<IConditionData>, IClonable<ICondition>
    {
    }
}
