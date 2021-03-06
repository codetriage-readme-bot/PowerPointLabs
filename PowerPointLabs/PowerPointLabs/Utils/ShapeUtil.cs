using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;

using PowerPointLabs.Models;

using Shape = Microsoft.Office.Interop.PowerPoint.Shape;
using ShapeRange = Microsoft.Office.Interop.PowerPoint.ShapeRange;
using TextFrame2 = Microsoft.Office.Interop.PowerPoint.TextFrame2;

namespace PowerPointLabs.Utils
{
    internal static class ShapeUtil
    {
#pragma warning disable 0618
        #region Constants

        private const int MaxShapeNameLength = 255;

        #endregion

        #region API

        #region Shape Types

        #region Shape

        // TODO: This could be an extension method of shape.
        public static bool IsHidden(Shape shape)
        {
            return shape.Visible == MsoTriState.msoFalse;
        }

        public static bool IsAGroup(Shape shape)
        {
            return shape.Type == MsoShapeType.msoGroup;
        }

        public static bool IsAChild(Shape shape)
        {
            try
            {
                Shape parent = shape.ParentGroup;
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // Expected exception thrown if shape does not have a parent
                return false;
            }
        }
        
        public static bool IsStraightLine(Shape shape)
        {
            return shape.Type == MsoShapeType.msoLine ||
                    (shape.Type == MsoShapeType.msoAutoShape &&
                     shape.AutoShapeType == MsoAutoShapeType.msoShapeMixed &&
                     shape.ConnectorFormat.Type == MsoConnectorType.msoConnectorStraight);
        }

        public static bool IsShape(Shape shape)
        {
            return shape.Type == MsoShapeType.msoAutoShape
                || shape.Type == MsoShapeType.msoFreeform
                || shape.Type == MsoShapeType.msoGroup;
        }

        public static bool IsPicture(Shape shape)
        {
            return shape.Type == MsoShapeType.msoPicture ||
                   shape.Type == MsoShapeType.msoLinkedPicture;
        }

        public static bool IsPictureOrShape(Shape shape)
        {
            return IsPicture(shape) || IsShape(shape);
        }

        public static bool IsSameType(Shape refShape, Shape candidateShape)
        {
            return refShape != null &&
                   candidateShape != null &&
                   refShape.Type == candidateShape.Type &&
                   (refShape.Type != MsoShapeType.msoAutoShape ||
                   refShape.AutoShapeType == candidateShape.AutoShapeType);
        }

        public static bool CanAddArrows(Shape shape)
        {
            try
            {
                if (shape.Line.Visible != MsoTriState.msoTrue)
                {
                    return false;
                }
                MsoArrowheadStyle style = shape.Line.BeginArrowheadStyle;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        #endregion

        #region ShapeRange

        public static bool IsAllPictureOrShape(ShapeRange shapeRange)
        {
            return (from Shape shape in shapeRange select shape).All(IsPictureOrShape);
        }

        public static bool IsAllPicture(ShapeRange shapeRange)
        {
            return (from Shape shape in shapeRange select shape).All(IsPicture);
        }

        public static bool IsAllPictureWithReferenceObject(ShapeRange shapeRange)
        {
            if (!IsPictureOrShape(shapeRange[1]))
            {
                return false;
            }
            for (int i = 2; i <= shapeRange.Count; i++)
            {
                if (!IsPicture(shapeRange[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsAllShape(ShapeRange shapeRange)
        {
            return (from Shape shape in shapeRange select shape).All(IsShape);
        }

        #endregion

        #region Selection

        public static bool IsSelectionShape(Selection selection)
        {
            return selection.Type == PpSelectionType.ppSelectionShapes &&
                    selection.ShapeRange.Count >= 1;
        }

        public static bool IsSelectionShapeOrText(Selection selection)
        {
            if (selection.Type != PpSelectionType.ppSelectionShapes &&
                selection.Type != PpSelectionType.ppSelectionText)
            {
                return false;
            }

            foreach (Shape shape in selection.ShapeRange)
            {
                if (shape.Type == MsoShapeType.msoPlaceholder)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HasPlaceholderInSelection(Selection selection)
        {
            if (selection.Type != PpSelectionType.ppSelectionShapes)
            {
                return false;
            }

            foreach (Shape shape in selection.ShapeRange)
            {
                if (shape.Type == MsoShapeType.msoPlaceholder)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSelectionSingleShape(Selection selection)
        {
            if (selection.Type != PpSelectionType.ppSelectionShapes)
            {
                return false;
            }

            if (selection.HasChildShapeRange)
            {
                return selection.ChildShapeRange.Count == 1;
            }

            return selection.ShapeRange.Count == 1;
        }

        public static bool IsSelectionMultipleOrGroup(Selection selection)
        {
            if (selection.Type != PpSelectionType.ppSelectionShapes)
            {
                return false;
            }

            if (selection.ShapeRange.Count > 1)
            {
                return true;
            }

            if (IsAGroup(selection.ShapeRange[1]))
            {
                return true;
            }

            return false;
        }

        public static bool IsSelectionMultipleSameShapeType(Selection selection)
        {
            bool result = false;

            if (selection.Type != PpSelectionType.ppSelectionShapes)
            {
                return result;
            }

            Shape shape = selection.ShapeRange[1];

            if (selection.ShapeRange.Count > 1)
            {
                foreach (Shape tempShape in selection.ShapeRange)
                {
                    if (shape.Type == tempShape.Type)
                    {
                        result = true;
                    }
                    if (shape.Type == MsoShapeType.msoAutoShape && shape.AutoShapeType != tempShape.AutoShapeType)
                    {
                        result = false;
                        break;
                    }
                }
            }
            return result;
        }

        public static bool IsSelectionAllRectangle(Selection selection)
        {
            if (selection.Type != PpSelectionType.ppSelectionShapes)
            {
                return false;
            }

            ShapeRange shapes = selection.ShapeRange;
            foreach (Shape shape in shapes)
            {
                if ((shape.Type != MsoShapeType.msoAutoShape ||
                   shape.AutoShapeType != MsoAutoShapeType.msoShapeRectangle) &&
                   shape.Type != MsoShapeType.msoPicture)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsSelectionAllShapeWithArea(Selection selection)
        {
            if (selection.Type != PpSelectionType.ppSelectionShapes)
            {
                return false;
            }

            ShapeRange shapes = selection.ShapeRange;
            foreach (Shape shape in shapes)
            {
                if (shape.Type != MsoShapeType.msoAutoShape && shape.Type != MsoShapeType.msoFreeform &&
                    shape.Type != MsoShapeType.msoTextBox && shape.Type != MsoShapeType.msoPlaceholder &&
                    shape.Type != MsoShapeType.msoCallout && shape.Type != MsoShapeType.msoInk &&
                    shape.Type != MsoShapeType.msoGroup)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsSelectionChildShapeRange(Selection selection)
        {
            return selection.HasChildShapeRange;
        }

        #endregion

        #endregion

        #region Shape Name

        public static bool IsShapeNameOverMaximumLength(string shapeName)
        {
            return shapeName.Length > MaxShapeNameLength;
        }

        // TODO: This could be an extension method of shape.
        public static bool HasDefaultName(Shape shape)
        {
            var copy = shape.Duplicate()[1];
            bool hasDefaultName = copy.Name != shape.Name;
            copy.Delete();
            return hasDefaultName;
        }

        #endregion

        #region Corruption Handling

        public static bool IsCorrupted(Shape shape)
        {
            try
            {
                return shape.Parent == null;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public static Shape CorruptionCorrection(Shape shape, PowerPointSlide ownerSlide)
        {
            // in case of random corruption of shape, cut-paste a shape before using its property
            Shape correctedShape = ownerSlide.CopyShapeToSlide(shape);
            shape.Delete();
            return correctedShape;
        }

        public static ShapeRange CorruptionCorrection(ShapeRange shapes, PowerPointSlide ownerSlide)
        {
            List<Shape> correctedShapeList = new List<Shape>();
            foreach (Shape shape in shapes)
            {
                Shape correctedShape = CorruptionCorrection(shape, ownerSlide);
                correctedShapeList.Add(correctedShape);
            }
            return ownerSlide.ToShapeRange(correctedShapeList);
        }

        #endregion

        #region Size and Position

        public static bool IsSamePosition(Shape refShape, Shape candidateShape,
                                          bool exactMatch = true, float blurRadius = float.Epsilon)
        {
            if (exactMatch)
            {
                blurRadius = float.Epsilon;
            }

            return refShape != null &&
                   candidateShape != null &&
                   Math.Abs(refShape.Left - candidateShape.Left) < blurRadius &&
                   Math.Abs(refShape.Top - candidateShape.Top) < blurRadius;
        }

        public static bool IsSameSize(Shape refShape, Shape candidateShape,
                                      bool exactMatch = true, float blurRadius = float.Epsilon)
        {
            if (exactMatch)
            {
                blurRadius = float.Epsilon;
            }

            return refShape != null &&
                   candidateShape != null &&
                   Math.Abs(refShape.Width - candidateShape.Width) < blurRadius &&
                   Math.Abs(refShape.Height - candidateShape.Height) < blurRadius;
        }

        // TODO: This could be an extension method of shape.
        public static float GetMidpointX(Shape shape)
        {
            return shape.Left + shape.Width / 2;
        }

        // TODO: This could be an extension method of shape.
        public static void SetMidpointX(Shape shape, float value)
        {
            shape.Left = value - shape.Width / 2;
        }

        // TODO: This could be an extension method of shape.
        public static float GetMidpointY(Shape shape)
        {
            return shape.Top + shape.Height / 2;
        }

        // TODO: This could be an extension method of shape.
        public static void SetMidpointY(Shape shape, float value)
        {
            shape.Top = value - shape.Height / 2;
        }

        // TODO: This could be an extension method of shape.
        public static float GetRight(Shape shape)
        {
            return shape.Left + shape.Width;
        }

        // TODO: This could be an extension method of shape.
        public static void SetRight(Shape shape, float value)
        {
            shape.Left = value - shape.Width;
        }

        // TODO: This could be an extension method of shape.
        public static float GetBottom(Shape shape)
        {
            return shape.Top + shape.Height;
        }

        // TODO: This could be an extension method of shape.
        public static void SetBottom(Shape shape, float value)
        {
            shape.Top = value - shape.Height;
        }

        public static float GetScaleWidth(Shape shape)
        {
            if (IsShape(shape))
            {
                return 1.0f;
            }

            var isAspectRatioLocked = shape.LockAspectRatio;
            shape.LockAspectRatio = MsoTriState.msoFalse;

            float oldWidth = shape.Width;
            shape.ScaleWidth(1, MsoTriState.msoCTrue);
            float scaleFactorWidth = oldWidth / shape.Width;

            shape.ScaleWidth(scaleFactorWidth, MsoTriState.msoCTrue);

            shape.LockAspectRatio = isAspectRatioLocked;
            return scaleFactorWidth;
        }

        public static float GetScaleHeight(Shape shape)
        {
            if (IsShape(shape))
            {
                return 1.0f;
            }

            var isAspectRatioLocked = shape.LockAspectRatio;
            shape.LockAspectRatio = MsoTriState.msoFalse;

            float oldHeight = shape.Height;
            shape.ScaleHeight(1, MsoTriState.msoCTrue);
            float scaleFactorHeight = oldHeight / shape.Height;

            shape.ScaleHeight(scaleFactorHeight, MsoTriState.msoCTrue);

            shape.LockAspectRatio = isAspectRatioLocked;
            return scaleFactorHeight;
        }

        public static PointF GetCenterPoint(Shape s)
        {
            return new PointF(s.Left + s.Width / 2, s.Top + s.Height / 2);
        }

        // TODO: This could be an extension method of shape.
        /// <summary>
        /// anchorFraction = 0 means left side, anchorFraction = 1 means right side.
        /// </summary>
        public static void SetShapeX(Shape shape, float value, float anchorFraction)
        {
            shape.Left = value - shape.Width * anchorFraction;
        }

        /// <summary>
        /// anchorFraction = 0 means top side, anchorFraction = 1 means bottom side.
        /// </summary>
        public static void SetShapeY(Shape shape, float value, float anchorFraction)
        {
            shape.Top = value - shape.Height * anchorFraction;
        }

        #endregion

        #region ZOrder

        /// <summary>
        /// Sort by increasing Z-Order.
        /// (From front to back).
        /// </summary>
        public static void SortByZOrder(List<Shape> shapes)
        {
            shapes.Sort((sh1, sh2) => sh2.ZOrderPosition - sh1.ZOrderPosition);
        }

        /// <summary>
        /// Moves shiftShape until it is at destination zOrder index.
        /// </summary>
        public static void MoveZTo(Shape shiftShape, int destination)
        {
            while (shiftShape.ZOrderPosition < destination)
            {
                int currentValue = shiftShape.ZOrderPosition;
                shiftShape.ZOrder(MsoZOrderCmd.msoBringForward);
                if (shiftShape.ZOrderPosition == currentValue)
                {
                    // Break if no change. Guards against infinite loops.
                    break;
                }
            }

            while (shiftShape.ZOrderPosition > destination)
            {
                int currentValue = shiftShape.ZOrderPosition;
                shiftShape.ZOrder(MsoZOrderCmd.msoSendBackward);
                if (shiftShape.ZOrderPosition == currentValue)
                {
                    // Break if no change. Guards against infinite loops.
                    break;
                }
            }
        }

        /// <summary>
        /// Moves shiftShape forward until it is in front of destinationShape.
        /// (does nothing if already in front)
        /// </summary>
        public static void MoveZUntilInFront(Shape shiftShape, Shape destinationShape)
        {
            while (shiftShape.ZOrderPosition < destinationShape.ZOrderPosition)
            {
                int currentValue = shiftShape.ZOrderPosition;
                shiftShape.ZOrder(MsoZOrderCmd.msoBringForward);
                if (shiftShape.ZOrderPosition == currentValue)
                {
                    // Break if no change. Guards against infinite loops.
                    break;
                }
            }
        }

        /// <summary>
        /// Moves shiftShape backward until it is behind destinationShape.
        /// (does nothing if already behind)
        /// </summary>
        public static void MoveZUntilBehind(Shape shiftShape, Shape destinationShape)
        {
            while (shiftShape.ZOrderPosition > destinationShape.ZOrderPosition)
            {
                int currentValue = shiftShape.ZOrderPosition;
                shiftShape.ZOrder(MsoZOrderCmd.msoSendBackward);
                if (shiftShape.ZOrderPosition == currentValue)
                {
                    // Break if no change. Guards against infinite loops.
                    break;
                }
            }
        }

        public static void SwapZOrder(Shape originalShape, Shape destinationShape)
        {
            Shape lowerZOrderShape = originalShape;
            Shape higherZOrderShape = destinationShape;
            if (originalShape.ZOrderPosition > destinationShape.ZOrderPosition)
            {
                lowerZOrderShape = destinationShape;
                higherZOrderShape = originalShape;
            }
            int lowerZOrder = lowerZOrderShape.ZOrderPosition;
            int higherZOrder = higherZOrderShape.ZOrderPosition;

            // If shape is a group, our target zOrder needs to be offset by the number of items in the group
            // This is to account for the zOrder of the items in the group
            if (IsAGroup(lowerZOrderShape))
            {
                higherZOrder -= lowerZOrderShape.GroupItems.Count;
            }

            if (IsAGroup(higherZOrderShape))
            {
                higherZOrder += higherZOrderShape.GroupItems.Count;
            }

            MoveZTo(lowerZOrderShape, higherZOrder);

            MoveZTo(higherZOrderShape, lowerZOrder);
        }

        /// <summary>
        /// Moves shiftShape forward/backward until it is just behind destinationShape
        /// </summary>
        public static void MoveZToJustBehind(Shape shiftShape, Shape destinationShape)
        {
            // Step 1: Shift forward until it overshoots destination.
            MoveZUntilInFront(shiftShape, destinationShape);

            // Step 2: Shift backward until it overshoots destination.
            MoveZUntilBehind(shiftShape, destinationShape);
        }

        /// <summary>
        /// Moves shiftShape forward/backward until it is just in front of destinationShape
        /// </summary>
        public static void MoveZToJustInFront(Shape shiftShape, Shape destinationShape)
        {
            // Step 1: Shift backward until it overshoots destination.
            MoveZUntilBehind(shiftShape, destinationShape);

            // Step 2: Shift forward until it overshoots destination.
            MoveZUntilInFront(shiftShape, destinationShape);
        }

        #endregion

        #region Transformations

        public static void FitShapeToSlide(ref Shape shapeToMove)
        {
            shapeToMove.LockAspectRatio = MsoTriState.msoFalse;
            shapeToMove.Left = 0;
            shapeToMove.Top = 0;
            shapeToMove.Width = PowerPointPresentation.Current.SlideWidth;
            shapeToMove.Height = PowerPointPresentation.Current.SlideHeight;
        }

        /// <summary>
        /// anchorX and anchorY are between 0 and 1. They represent the pivot to rotate the shape about.
        /// The shape rotates by angle difference angle from its current angle. angle is in degrees.
        /// </summary>
        public static void RotateShapeAboutPivot(Shape shape, float angle, float anchorX, float anchorY)
        {
            double pivotX = shape.Left + anchorX * shape.Width;
            double pivotY = shape.Top + anchorY * shape.Height;
            double midpointX = GetMidpointX(shape);
            double midpointY = GetMidpointY(shape);

            double dx = midpointX - pivotX;
            double dy = midpointY - pivotY;

            double radAngle = angle * Math.PI / 180;
            double newdx = Math.Cos(radAngle) * dx - Math.Sin(radAngle) * dy;
            double newdy = Math.Sin(radAngle) * dx + Math.Cos(radAngle) * dy;

            SetMidpointX(shape, (float)(pivotX + newdx));
            SetMidpointY(shape, (float)(pivotY + newdy));
            shape.Rotation += angle;
        }

        #endregion

        #region Shape Finding

        public static ShapeRange GetShapesWhenTypeMatches(PowerPointSlide slide, ShapeRange shapes, MsoShapeType type)
        {
            List<Shape> newShapeList = new List<Shape>();
            foreach (Shape shape in shapes)
            {
                if (shape.Type == type)
                {
                    newShapeList.Add(shape);
                }
            }
            return slide.ToShapeRange(newShapeList);
        }

        public static ShapeRange GetShapesWhenTypeNotMatches(PowerPointSlide slide, ShapeRange shapes, MsoShapeType type)
        {
            List<Shape> newShapeList = new List<Shape>();
            foreach (Shape shape in shapes)
            {
                if (shape.Type != type)
                {
                    newShapeList.Add(shape);
                }
            }
            return slide.ToShapeRange(newShapeList);
        }

        public static List<Shape> GetChildrenWithNonEmptyTag(Shape shape, string tagName)
        {
            List<Shape> result = new List<Shape>();

            if (!IsAGroup(shape))
            {
                return result;
            }

            for (int i = 1; i <= shape.GroupItems.Count; i++)
            {
                Shape child = shape.GroupItems.Range(i)[1];
                if (!child.Tags[tagName].Equals(""))
                {
                    result.Add(child);
                }
            }
            return result;
        }

        public static List<PPShape> SortShapesByLeft(List<PPShape> selectedShapes)
        {
            List<PPShape> shapesToBeSorted = new List<PPShape>();

            for (int i = 0; i < selectedShapes.Count; i++)
            {
                shapesToBeSorted.Add(selectedShapes[i]);
            }

            shapesToBeSorted.Sort(LeftComparator);

            return shapesToBeSorted;
        }

        public static List<PPShape> SortShapesByTop(List<PPShape> selectedShapes)
        {
            List<PPShape> shapesToBeSorted = new List<PPShape>();

            for (int i = 0; i < selectedShapes.Count; i++)
            {
                shapesToBeSorted.Add(selectedShapes[i]);
            }

            shapesToBeSorted.Sort(TopComparator);

            return shapesToBeSorted;
        }
        
        #endregion

        #region Animations

        public static void MakeShapeViewTimeInvisible(Shape shape, Slide curSlide)
        {
            var sequence = curSlide.TimeLine.MainSequence;

            var effectAppear = sequence.AddEffect(shape, MsoAnimEffect.msoAnimEffectAppear,
                                                  MsoAnimateByLevel.msoAnimateLevelNone,
                                                  MsoAnimTriggerType.msoAnimTriggerWithPrevious);
            effectAppear.Timing.Duration = 0;

            var effectDisappear = sequence.AddEffect(shape, MsoAnimEffect.msoAnimEffectAppear,
                                                     MsoAnimateByLevel.msoAnimateLevelNone,
                                                     MsoAnimTriggerType.msoAnimTriggerWithPrevious);
            effectDisappear.Exit = MsoTriState.msoTrue;
            effectDisappear.Timing.Duration = 0;

            effectAppear.MoveTo(1);
            effectDisappear.MoveTo(2);
        }

        public static void MakeShapeViewTimeInvisible(Shape shape, PowerPointSlide curSlide)
        {
            MakeShapeViewTimeInvisible(shape, curSlide.GetNativeSlide());
        }

        public static void MakeShapeViewTimeInvisible(ShapeRange shapeRange, Slide curSlide)
        {
            foreach (Shape shape in shapeRange)
            {
                MakeShapeViewTimeInvisible(shape, curSlide);
            }
        }

        public static void MakeShapeViewTimeInvisible(ShapeRange shapeRange, PowerPointSlide curSlide)
        {
            MakeShapeViewTimeInvisible(shapeRange, curSlide.GetNativeSlide());
        }

        #endregion

        #region Syncing

        /// <summary>
        /// A better version of SyncShape, but cannot do a partial sync like SyncShape can.
        /// SyncShape cannot operate on Groups and Charts. If those are detected, SyncWholeShape resorts to deleting
        /// candidateShape and replacing it with a copy of refShape instead of syncing.
        /// </summary>
        public static void SyncWholeShape(Shape refShape, ref Shape candidateShape, PowerPointSlide candidateSlide)
        {
            bool succeeded = true;
            try
            {
                SyncShape(refShape, candidateShape);
            }
            catch (UnauthorizedAccessException)
            {
                succeeded = false;
            }
            catch (ArgumentException)
            {
                succeeded = false;
            }
            catch (COMException)
            {
                succeeded = false;
            }
            if (succeeded)
            {
                return;
            }

            candidateShape.Delete();
            refShape.Copy();
            candidateShape = candidateSlide.Shapes.Paste()[1];
            candidateShape.Name = refShape.Name;
        }

        public static void SyncShape(Shape refShape, Shape candidateShape,
                                     bool pickupShapeBasic = true, bool pickupShapeFormat = true,
                                     bool pickupTextContent = true, bool pickupTextFormat = true)
        {
            if (pickupShapeBasic)
            {
                SyncShapeRotation(refShape, candidateShape);
                SyncShapeSize(refShape, candidateShape);
                SyncShapeLocation(refShape, candidateShape);
            }


            if (pickupShapeFormat)
            {
                refShape.PickUp();
                candidateShape.Apply();
            }

            if ((pickupTextContent || pickupTextFormat) &&
                refShape.HasTextFrame == MsoTriState.msoTrue &&
                candidateShape.HasTextFrame == MsoTriState.msoTrue)
            {
                var refTextRange = refShape.TextFrame2.TextRange;
                var candidateTextRange = candidateShape.TextFrame2.TextRange;

                if (pickupTextContent)
                {
                    candidateTextRange.Text = refTextRange.Text;
                }

                var refParagraphCount = refShape.TextFrame2.TextRange.Paragraphs.Count;
                var candidateParagraphCount = candidateShape.TextFrame2.TextRange.Paragraphs.Count;

                if (refParagraphCount > 0)
                {
                    string originalText = candidateTextRange.Text;
                    SyncTextRange(refTextRange.Paragraphs[refParagraphCount], candidateTextRange);
                    candidateTextRange.Text = originalText;
                }

                for (var i = 1; i <= candidateParagraphCount; i++)
                {
                    var refParagraph = refTextRange.Paragraphs[i <= refParagraphCount ? i : refParagraphCount];
                    var candidateParagraph = candidateTextRange.Paragraphs[i];

                    SyncTextRange(refParagraph, candidateParagraph, pickupTextContent, pickupTextFormat);
                }
            }
        }

        public static void SyncShapeRange(ShapeRange refShapeRange, ShapeRange candidateShapeRange)
        {
            // all names of identical shapes should be consistent
            if (refShapeRange.Count != candidateShapeRange.Count)
            {
                return;
            }

            foreach (var shape in candidateShapeRange)
            {
                var candidateShape = shape as Shape;
                var refShape = refShapeRange.Cast<Shape>().FirstOrDefault(item => IsSameType(item, candidateShape) &&
                                                                                  IsSamePosition(item, candidateShape,
                                                                                                 false, 15) &&
                                                                                  IsSameSize(item, candidateShape));

                if (candidateShape == null || refShape == null)
                {
                    continue;
                }

                candidateShape.Name = refShape.Name;
            }
        }

        public static void SyncTextRange(TextRange2 refTextRange, TextRange2 candidateTextRange,
                                         bool pickupTextContent = true, bool pickupTextFormat = true)
        {
            bool originallyHadNewLine = candidateTextRange.Text.EndsWith("\r");
            bool lostTheNewLine = false;

            var candidateText = candidateTextRange.Text.TrimEnd('\r');

            if (pickupTextFormat)
            {
                // pick up format using copy-paste, since we could not deep copy the format
                refTextRange.Copy();
                candidateTextRange.PasteSpecial(MsoClipboardFormat.msoClipboardFormatNative);
                lostTheNewLine = !candidateTextRange.Text.EndsWith("\r");
            }

            if (!pickupTextContent)
            {
                candidateTextRange.Text = candidateText;

                // Handling an uncommon edge case. If we are not copying paragraph content, only format,
                // Sometimes (when the reference paragraph doesn't end with a newline), the newline will be lost after copy.
                if (originallyHadNewLine && lostTheNewLine)
                {
                    candidateTextRange.Text = candidateTextRange.Text + "\r";
                }
            }

            if (refTextRange.Text.Trim().Equals(""))
            {
                candidateTextRange.Text = " ";
            }
        }

        #endregion

        #region Shape Text

        public static void DeleteTagFromShapes(ShapeRange shapes, string tagName)
        {
            foreach (Shape shape in shapes)
            {
                shape.Tags.Delete(tagName);
            }
        }

        // TODO: This could be an extension method of shape.
        public static string GetText(Shape shape)
        {
            return shape.TextFrame2.TextRange.Text;
        }

        // TODO: This could be an extension method of shape.
        public static void SetText(Shape shape, params string[] lines)
        {
            shape.TextFrame2.TextRange.Text = string.Join("\r", lines);
        }

        // TODO: This could be an extension method of shape.
        public static void SetText(Shape shape, IEnumerable<string> lines)
        {
            shape.TextFrame2.TextRange.Text = string.Join("\r", lines);
        }

        // TODO: This could be an extension method of shape.
        /// <summary>
        /// Get the paragraphs of the shape as a list.
        /// The paragraphs formats can be modified to change the format of the paragraphs in shape.
        /// This list is 0-indexed.
        /// </summary>
        public static List<TextRange2> GetParagraphs(Shape shape)
        {
            return shape.TextFrame2.TextRange.Paragraphs.Cast<TextRange2>().ToList();
        }

        public static TextRange ConvertTextRange2ToTextRange(TextRange2 textRange2)
        {
            var textFrame2 = textRange2.Parent as TextFrame2;

            if (textFrame2 == null)
            {
                return null;
            }

            var shape = textFrame2.Parent as Shape;

            return shape == null ? null : shape.TextFrame.TextRange;
        }

        #endregion

        #endregion

        #region Helper Methods
        private static void SyncShapeLocation(Shape refShape, Shape candidateShape)
        {
            candidateShape.Left = refShape.Left;
            candidateShape.Top = refShape.Top;
        }

        private static void SyncShapeRotation(Shape refShape, Shape candidateShape)
        {
            candidateShape.Rotation = refShape.Rotation;
        }

        private static void SyncShapeSize(Shape refShape, Shape candidateShape)
        {
            // unlock aspect ratio to enable size tweak
            var candidateLockRatio = candidateShape.LockAspectRatio;

            candidateShape.LockAspectRatio = MsoTriState.msoFalse;

            candidateShape.Width = refShape.Width;
            candidateShape.Height = refShape.Height;

            candidateShape.LockAspectRatio = candidateLockRatio;
        }
        #endregion

        #region Comparators

        private static int LeftComparator(PPShape s1, PPShape s2)
        {
            return s1.VisualLeft.CompareTo(s2.VisualLeft);
        }

        private static int TopComparator(PPShape s1, PPShape s2)
        {
            return s1.VisualTop.CompareTo(s2.VisualTop);
        }

        #endregion
    }
}
