﻿using System;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Sources
{
    /// <summary>
    /// Tweets source of status
    /// </summary>
    public abstract class FilterSourceBase : IFilterQueryElement
    {
        public abstract string ToQuery();

        public abstract Func<TwitterStatus, bool> GetEvaluator();

        /// <summary>
        /// Activate dependency receiving method.
        /// </summary>
        public virtual void Activate() { }

        /// <summary>
        /// Deactivate dependency receiving method.
        /// </summary>
        public virtual void Deactivate() { }
    }
}
