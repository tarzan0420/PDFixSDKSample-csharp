﻿////////////////////////////////////////////////////////////////////////////////////////////////////
// TagAsArtifact.cs
// Copyright (c) 2019 Pdfix. All Rights Reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////
/*!
\page CS_Samples C# Samples
- \subpage TagAsArtifact_cs
*/
/*!
\page TagHeadings_cpp Tag paragraph as Heading
Example how to manually edit tags tagged P struct element to H1 based on the text prperties.
\snippet /TagAsArtifact.cs TagAsArtifact_cs
*/

//\cond INTERNAL
//! [TagAsArtifact_cs]
using System;
using PDFixSDK.Pdfix;

namespace PDFix.App.Module
{
    class TagHeadings
    {
        internal static Pdfix pdfix;

        //////////////////////////////////////////////////////////////////////////////////////////////////
        // GetPageObjectTextState
        // get text object's of specified mcid the text state
        //////////////////////////////////////////////////////////////////////////////////////////////////
        static PdfTextState GetPageObjectTextState(PdsPageObject page_object, int mcid)
        {
            if (page_object.GetObjectType() == PdfPageObjectType.kPdsPageText)
            {
                PdsText text = (PdsText)page_object;

                // check if this text page object has the same mcid
                PdsContentMark content_mark = page_object.GetContentMark();

                var tag_magicId = content_mark.GetTagMcid();
                Console.WriteLine("tag_magicId : ", tag_magicId);

                if (content_mark != null && content_mark.GetTagMcid() == mcid)
                {
                    PdfTextState ts = text.GetTextState(page_object.GetPage().GetDoc());
                    if (ts.font != null)
                        return ts;
                }
            }
            else if (page_object.GetObjectType() == PdfPageObjectType.kPdsPageForm)
            {
                // search for the text object inside of the form XObject
                PdsForm form = (PdsForm)page_object;
                for (int i = 0; i < form.GetNumPageObjects(); i++)
                {
                    var ts = GetPageObjectTextState(form.GetPageObject(i), mcid);
                    if (ts.font != null)
                        return ts;
                }
            }
            return new PdfTextState();
        }        

        //////////////////////////////////////////////////////////////////////////////////////////////////
        // GetParagraphTextState
        // get the text state of the text objects inside paragraph by iterating content kid objects
        //////////////////////////////////////////////////////////////////////////////////////////////////
        static PdfTextState GetParagraphTextState(PdsStructElement struct_elem)
        {
            for (int i = 0; i < struct_elem.GetNumKids(); i++)
            {
                if (struct_elem.GetKidType(i) == PdfStructElementType.kPdsStructKidPageContent)
                {
                    // acquire page on which the element is present
                    PdfDoc doc = struct_elem.GetStructTree().GetDoc();
                    PdfPage page = doc.AcquirePage(struct_elem.GetKidPageNumber(i));

                    // find text object with mcid on the page to get the text state
                    int mcid = struct_elem.GetKidMcid(i);
                    var num_pages = page.GetNumPageObjects();

                    for (int j = 0; j < page.GetNumPageObjects(); j++)
                    {
                        var ts = GetPageObjectTextState(page.GetPageObject(j), mcid);


                        // Handled by MIkhaylov KS
                        if (ts.font_size == 0)
                            continue;

                        page.Release();
                        return ts;
                    }
                    page.Release();
                }
            }
            return new PdfTextState();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////
        // TagParagraphAsHeading
        // re-tag the struct element to heading based on font properties
        //////////////////////////////////////////////////////////////////////////////////////////////////
        static void TagParagraphAsHeading(PdsStructElement struct_elem)
        {
            string type = struct_elem.GetType_(true);
            // if (type == "P") {
                // get the paragraph text_state
                PdfTextState ts = GetParagraphTextState(struct_elem);

                // get the font name
                //string font_name = ts.font != null ? ts.font.GetFontName() : "";
                string tag_type = "";


                //if (font_name.Contains("Black") && ts.font_size >= 25)
                //    tag_type = "H1";
                //else if (font_name.Contains("Bold") && ts.font_size >= 16)
                //    tag_type = "H2";

                Console.WriteLine("Before replacing..");

                if (ts.font_size >= 14)
                {
                    tag_type = "H1";
                    Console.WriteLine("Replacing paragraph with Heading 1");
                }

                else if (ts.font_size >= 12)
                {
                    tag_type = "H2";
                    Console.WriteLine("Replacing paragraph with Heading 2");
                }

                else if (ts.font_size >= 6)
                {
                    tag_type = "H3";
                    Console.WriteLine("Replacing paragraph with Heading 2");
                }


                // update tag type
                if (tag_type.Length != 0)
                {
                    struct_elem.SetType(tag_type);
                }
                // return; // this was a P tag, no need to continue to kid struct elements
            //}

            // search kid struct elements
            for (int i = 0; i < struct_elem.GetNumKids(); i++)
            {
                string actualText = struct_elem.GetActualText();

                if (struct_elem.GetKidType(i) == PdfStructElementType.kPdsStructKidElement)
                {
                    PdsObject kid_obj = struct_elem.GetKidObject(i);
                    PdsStructElement kid_elem = struct_elem.GetStructTree().AcquireStructElement(kid_obj);

                    TagParagraphAsHeading(kid_elem);
                    kid_elem.Release();
                }
                else if (struct_elem.GetKidType(i) == PdfStructElementType.kPdsStructKidPageContent)
                {
                    PdfTextState textState = GetParagraphTextState(struct_elem);

                    //PdsObject kid_obj = 
                    //PdsStructElement kid_elem = struct_elem.GetStructTree().AcquireStructElement(kid_obj);
                    //var element_text = kid_elem.GetActualText();

                }
            }
        }

        public static void Run(
            String email,                               // authorization email   
            String licenseKey,                          // authorization license key
            String openPath,                            // source PDF document
            String savePath                             // dest PDF document
            )
        {
            pdfix = new Pdfix();
            if (pdfix == null)
                throw new Exception("Pdfix initialization fail");

            if (!pdfix.Authorize(email, licenseKey))
                throw new Exception(pdfix.GetErrorType().ToString());

            PdfDoc doc = pdfix.OpenDoc(openPath, "");
            if (doc == null)
                throw new Exception(pdfix.GetError());

            // cleanup any previous structure tree
            if (!doc.RemoveTags(null, IntPtr.Zero))
                throw new Exception(pdfix.GetErrorType().ToString());

            // autotag document first
            if (!doc.AddTags(null, IntPtr.Zero))
                throw new Exception(pdfix.GetErrorType().ToString());

            // get the struct tree
            PdsStructTree struct_tree = doc.GetStructTree();
            if (struct_tree == null)
                throw new Exception(pdfix.GetErrorType().ToString());

            // tag text on the bottom of the page as artifact
            for (int i = 0; i < struct_tree.GetNumKids(); i++)
            {
                PdsObject kid_obj = struct_tree.GetKidObject(i);
                PdsStructElement kid_elem = struct_tree.AcquireStructElement(kid_obj);
                TagParagraphAsHeading(kid_elem);
                kid_elem.Release();
            }

            if (!doc.Save(savePath, PdfSaveFlags.kSaveFull))
                throw new Exception(pdfix.GetError());

            doc.Close();
            pdfix.Destroy();
        }
    }
}
//! [TagAsArtifact_cs]
//\endcond