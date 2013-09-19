﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
	using System.Collections.Concurrent;
	using System.Threading;
	using Dinner;
	using Monitor = Dinner.Monitor;

	class Program
    {

		static  ConcurrentDictionary<Guid, object> orders = new ConcurrentDictionary<Guid, object>();

		static void Main(string[] args)
		{
			var d = new Dispatcher();
            var manager = new Manager();
            var cashier = new Cashier(d);
            var ass = new AssMan(d);

			var cookDispatcher = new SmartDispatcher<OrderPlaced>();
			var cookTTLGate = new TimeToLiveGate<OrderPlaced>(cookDispatcher);
			var cookQueudHandler = new QueuedHandler<OrderPlaced>(cookTTLGate, "dispatcher");
			var cookLimiter = new Limiter<OrderPlaced>(cookQueudHandler);
			
			d.Subscribe(cookLimiter);
			d.Subscribe(ass);
			d.Subscribe(cashier);
			d.Subscribe(manager);

			var cookQueudHandler1 = new QueuedHandler<OrderPlaced>(new Cook(d, 10000), "c1");
			cookDispatcher.AddHandler(cookQueudHandler1);
			var cookQueudHandler2 = new QueuedHandler<OrderPlaced>(new Cook(d, 1000), "c2");
			cookDispatcher.AddHandler(cookQueudHandler2);
			var cookQueudHandler3 = new QueuedHandler<OrderPlaced>(new Cook(d, 100), "c3");	
			cookDispatcher.AddHandler(cookQueudHandler3);

			var monitor = new Monitor(new[] {cookQueudHandler1, cookQueudHandler2, cookQueudHandler3, cookQueudHandler});
		
            //Cook cook = new Cook(ass);
            var waiter = new Waiter(d);

			cookQueudHandler1.Start();
			cookQueudHandler2.Start();
			cookQueudHandler3.Start();
			cookQueudHandler.Start();
			monitor.Start();
			new Thread(TryPay).Start(cashier);

            for (int i = 0; i < 100000; i++)
            {
                var orderNumber = waiter.PlaceOrder(new[] { Tuple.Create("Burger", 1) }, 15);
                orders.TryAdd(orderNumber, null);
		    }
            //var orderNumber = waiter.PlaceOrder(new[] {Tuple.Create("Burger", 1)}, 15);
            //cashier.PayForOrder(orderNumber);
            Console.ReadLine();
        }

		private static void TryPay(object c)

		{
			var cashier = (Cashier) c;
			while (true)
			{
				foreach (var order in orders)
				{
					try
					{
						cashier.PayForOrder(order.Key);
						object val;
						orders.TryRemove(order.Key,out val);
					}
					catch
					{
					}
				}
			}
		}
    }

    
}
