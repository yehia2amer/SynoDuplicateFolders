﻿using System.Collections.Generic;
using System.Drawing;

namespace SynoDuplicateFolders.Controls
{
    public interface IChartConfiguration
    {
        IChartLegend this[string key]
        {
            get;
        }

        bool ContainsKey(string key);
        IChartLegend Add(string key, Color k, bool forceKnownColor= false);
        IChartLegend Add(string key, KnownColor k);

        List<ITaggedColor> Pallete { get; }
        List<ITaggedColor> List { get; }
    }
    public interface ITaggedColor
    {
        string Key { get; set; }
        string ColorName { get; set; }
    }
    public interface IChartLegend : ITaggedColor
    {
        Color DefaultColor { get; }
        Color Color { get; set; }
    }

}
