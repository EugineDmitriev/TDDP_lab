//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: ServiceTutorial1.cs $ $Revision: 6 $
//-----------------------------------------------------------------------

#region CODECLIP Appendix A-6
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
#region CODECLIP 04-1
using Microsoft.Dss.Core.DsspHttp;
#endregion
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;

using System.Diagnostics;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
#endregion

using servicetutorial1 = RoboticsServiceTutorial1;


namespace RoboticsServiceTutorial1
{

    public class InputData
    {
        public double a;
        public int steps;
    }


    #region CODECLIP Appendix A-7
    /// <summary>
    /// Implementation class for ServiceTutorial1
    /// </summary>
    [DisplayName("(User) Service Tutorial 1: Creating a Service")]
    [Description("Demonstrates how to write a basic service.")]
    [Contract(Contract.Identifier)]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb483064.aspx")]
    public class ServiceTutorial1Service : DsspServiceBase
    {
    #endregion
        #region CODECLIP Appendix A-8
        /// <summary>
        /// Service State
        /// </summary>
        [ServiceState]
        private ServiceTutorial1State _state = new ServiceTutorial1State();
        #endregion

        #region CODECLIP Appendix A-9
        /// <summary>
        /// Main Port
        /// </summary>
        [ServicePort("/servicetutorial1", AllowMultipleInstances = false)]
        private ServiceTutorial1Operations _mainPort = new ServiceTutorial1Operations();
        #endregion

        #region CODECLIP Appendix A-10
        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public ServiceTutorial1Service(DsspServiceCreationPort creationPort) :
            base(creationPort)
        {
        }
        #endregion

        #region CODECLIP Appendix A-11
        /// <summary>
        /// Service Start
        /// </summary>
        /// 

        const int SIZE = 10000000;
        const int SIZE_THREAD = 10000;

        public double a;
        public double b;

        public static double h = 0.001;
        public double x = 0;

        double result = 0;
        double planeResult = 0;

        public static double f(double x)
        {
            return x * x * x - x * x + 6 + x;
        }

        public static void Calc(InputData data, Port<double> resp)
        {
            Stopwatch sWatch = new Stopwatch();

            double result = 0;
            sWatch.Start();

            for(int i = 0; i < data.steps; ++i)
                result += (f(data.a + i*h) + f(data.a + (i+1)*h)) * (h) / 2; 

            sWatch.Stop();

            Console.WriteLine("����� � {0}: ������. �������� = {1} ��.", System.Threading.Thread.CurrentThread.ManagedThreadId, sWatch.ElapsedMilliseconds.ToString());
            fullParallelTime += sWatch.ElapsedMilliseconds;
            resp.Post(result);
        }


        static long fullParallelTime = 0;

        protected override void Start()
        {
            base.Start();
            // Add service specific initialization here.

            Random r = new Random();

            a = -6;
            b = 9000;

            int parts = (int)((b - a) / h);
            Console.WriteLine("Parts: {0}", parts);

            System.Diagnostics.Stopwatch sp = new System.Diagnostics.Stopwatch();
            sp.Start();

            for(int i = 0; i < parts; ++i)
                planeResult += (f(a+h*i) + f(a+h*(i+1))) * (h) / 2;

            sp.Stop();

            string planeTime = sp.ElapsedMilliseconds.ToString();

            int nc = 400;

            InputData[] data = new InputData[nc];

            for (int i = 0; i < nc; ++i)
            {
                data[i] = new InputData();

                data[i].a = a + i*(parts/nc)*h;
                data[i].steps = parts / nc;
            }

            Dispatcher d = new Dispatcher(4, "Test Pool"); 
            DispatcherQueue dq = new DispatcherQueue("Test Queue", d);

            Port<double> port = new Port<double>();

            for (int i = 0; i < nc; i++) 
                Arbiter.Activate(dq, new Task<InputData, Port<double>>(data[i], port, Calc));

            Arbiter.Activate(Environment.TaskQueue, Arbiter.MultipleItemReceive(true, port, nc, delegate(double[] array) 
            {
                for (int i = 0; i < array.GetLength(0); ++i)
                    result += array[i];

                Console.WriteLine("Plane result: {0}", planeResult);
                Console.WriteLine("Parallel result: {0}", result);
               

                Console.WriteLine("Computation completed");
                Console.WriteLine("Parallel sorting time: {0}ms", fullParallelTime.ToString());
                Console.WriteLine("Linear sorting time: {0}ms", planeTime);
            }));

        }
        #endregion

        #region CODECLIP Appendix A-12
        /// <summary>
        /// Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        /// <remarks>This is a standard Get handler. It is not required because
        ///  DsspServiceBase will handle it if the state is marked with [ServiceState].
        ///  It is included here for illustration only.</remarks>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetHandler(Get get)
        {
            get.ResponsePort.Post(_state);
            yield break;
        }
        #endregion

        #region CODECLIP Appendix A-13
        /// <summary>
        /// Http Get Handler
        /// </summary>
        /// <remarks>This is a standard HttpGet handler. It is not required because
        ///  DsspServiceBase will handle it if the state is marked with [ServiceState].
        ///  It is included here for illustration only.</remarks>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public IEnumerator<ITask> HttpGetHandler(HttpGet httpGet)
        {
            httpGet.ResponsePort.Post(new HttpResponseType(_state));
            yield break;
        }
        #endregion

        #region CODECLIP Appendix A-14
        /// <summary>
        /// Replace Handler
        /// </summary>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public IEnumerator<ITask> ReplaceHandler(Replace replace)
        {
            _state = replace.Body;
            replace.ResponsePort.Post(DefaultReplaceResponseType.Instance);
            yield break;
        }
        #endregion
    }

}
