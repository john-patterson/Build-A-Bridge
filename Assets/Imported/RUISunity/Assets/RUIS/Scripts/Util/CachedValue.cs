/*****************************************************************************

Content    :   A class to cache values without having to recalculate the true value unnecessarily
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public abstract class CachedValue<T>
{
    private bool isValid = false;
    private T cachedValue;

    public void Invalidate()
    {
        isValid = false;
    }

    public T GetValue()
    {
        if (isValid) return cachedValue;

        cachedValue = CalculateValue();

        return cachedValue;
    }

    protected abstract T CalculateValue();
}
