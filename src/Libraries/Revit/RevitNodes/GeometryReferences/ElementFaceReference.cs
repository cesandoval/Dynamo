﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;
using RevitServices.Persistence;

namespace Revit.GeometryReferences
{
    /// <summary>
    /// A stable reference to a Revit Face, usually derived from a Revit Element
    /// </summary>
    /// See: http://revitapisearch.com.s3-website-us-east-1.amazonaws.com/html/f3d5d2fe-96bf-8528-4628-78d8d5e6705f.htm
    public class ElementFaceReference : ElementGeometryReference
    {
        internal ElementFaceReference(Autodesk.Revit.DB.Face face)
        {
            if (face.Reference == null)
            {
                throw new Exception("A Face Reference can only be obtained "
                                    + "from an Element.");
            }
            this.InternalReference = face.Reference;
        }

        internal static ElementFaceReference FromExisting(Autodesk.Revit.DB.Face arg)
        {
            return new ElementFaceReference(arg);
        }

        internal static ElementFaceReference TryGetFaceReference(object curveObject, string nodeTypeString = "This node")
        {
            var curve = (dynamic) curveObject;

            try
            {
                return TryGetFaceReference(curve);
            }
            catch (RuntimeBinderException e)
            {
                throw new ArgumentException(nodeTypeString +
                                            " requires a ElementFaceReference extracted from a Revit Element! ");
            }
            catch (Exception e)
            {
                throw new ArgumentException(nodeTypeString +
                                           " requires a ElementFaceReference extracted from a Revit Element! For some reason, we failed to obtain it.");
            }
        }

        private static ElementFaceReference TryGetFaceReference(ElementFaceReference curveObject)
        {
            return curveObject;
        }

        private static ElementFaceReference TryGetFaceReference(Revit.Elements.Element curveObject, string nodeTypeString = "This node")
        {
            var cs = curveObject.InternalGeometry().OfType<Autodesk.Revit.DB.Face>();
            if (cs.Any()) return new ElementFaceReference(cs.First());

            var ss = curveObject.InternalGeometry().OfType<Autodesk.Revit.DB.Solid>();
            if (ss.Any()) return new ElementFaceReference(ss.First().Faces.Cast<Autodesk.Revit.DB.Face>().First());

            throw new ArgumentException(nodeTypeString + " requires a ElementFaceReference extracted from a Revit Element! " +
                             "You supplied an " + curveObject.ToString() + ", but we could not extract a FaceReference from it!");
        }

        private static ElementFaceReference TryGetFaceReference(Autodesk.DesignScript.Geometry.Surface curveObject, string nodeTypeString = "This node")
        {
            throw new ArgumentException(nodeTypeString + " requires a ElementFaceReference extracted from a Revit Element! " +
                                         "You can use the ImportInstance.ByGeometry to " +
                                            "turn this Surface into a Revit Element.");
        }
    }

}
