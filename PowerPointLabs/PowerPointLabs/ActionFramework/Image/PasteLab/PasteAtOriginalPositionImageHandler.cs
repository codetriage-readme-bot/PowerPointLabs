﻿using System.Drawing;
using PowerPointLabs.ActionFramework.Common.Attribute;
using PowerPointLabs.ActionFramework.Common.Interface;

namespace PowerPointLabs.ActionFramework.Image.PasteLab
{
    [ExportImageRibbonId(
        "PasteAtOriginalPosition",
        "PasteAtOriginalPositionShape",
        "PasteAtOriginalPositionFreeform",
        "PasteAtOriginalPositionPicture",
        "PasteAtOriginalPositionGroup")]
    class PasteAtOriginalPositionImageHandler : ImageHandler
    {
        protected override Bitmap GetImage(string ribbonId)
        {
            return new Bitmap(Properties.Resources.ColorsLab);
        }
    }
}