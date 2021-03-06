////////////////////////////////////////////////////////////////////////////////////////////////////
// ThreadSafePdfix.cs
// Copyright (c) 2018 PDFix. All Rights Reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////

/*! 
\page CS_Samples C# Samples
- \subpage ThreadSafePdfix_cs
*/
/*! 
\page ThreadSafePdfix_cs Thread safe use Sample
// Example how to use PDFix SDK in a thread safe environment.
\snippet /ThreadSafePdfix.cs ThreadSafePdfix_cs
*/

//\cond INTERNAL
//! [ThreadSafePdfix_cs]
using System;
using PDFixSDK.Pdfix;
using System.Threading;

namespace PDFix.App.Module
{
    class ThreadSafePdfix
    {
        static Thread[] t = new Thread[4];
        static Semaphore semaphore = new Semaphore(1, 1);

        static PdfDoc doc = null;
        static Pdfix pdfix = null;

        static void DoSomething()
        {
            Console.WriteLine("{0} = waiting", Thread.CurrentThread.Name);
            semaphore.WaitOne();
            Console.WriteLine("{0} begins!", Thread.CurrentThread.Name);
            PdfPage page = doc.AcquirePage(0);
            if (page == null)
                throw new Exception(pdfix.GetError());

            Thread.Sleep(1000);
            page.Release();
            Console.WriteLine("{0} releasing...", Thread.CurrentThread.Name);
            semaphore.Release();
        }

        public static void Run(
            String email,                               // authorization email   
            String licenseKey,                          // authorization license key
            String openPath                             // source PDF document
            )
        {
            pdfix = new Pdfix();
            if (pdfix == null)
                throw new Exception("Pdfix initialization fail");

            if (!pdfix.Authorize(email, licenseKey))
                throw new Exception(pdfix.GetError());

            doc = pdfix.OpenDoc(openPath, "");
            if (doc == null)
                throw new Exception(pdfix.GetError());

            String[] op = new string[4];
            op[0] = "count pages";
            op[1] = "remove annotations";
            op[2] = "place watermark";
            op[3] = "extract table";

            for (int j = 0; j < 4; j++)
            {
                t[j] = new Thread(DoSomething);
                t[j].Name = op[j];
                t[j].Start();
            }

            for (int j = 0; j < 4; j++)
            {
                t[j].Join();
            }

            doc.Close();
            doc = null;
            pdfix.Destroy();
            pdfix = null;
        }
    }
}
//! [ThreadSafePdfix_cs]
//\endcond
