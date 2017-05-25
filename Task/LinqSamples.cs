// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SampleSupport;
using Task.Data;

// Version Mad01

namespace SampleQueries
{
	[Title("LINQ Module")]
	[Prefix("Linq")]
	public class LinqSamples : SampleHarness
	{

		private DataSource dataSource = new DataSource();

		[Category("Tasks")]
		[Title("1")]
		[Description("Выдайте список всех клиентов, чей суммарный оборот (сумма всех заказов) превосходит некоторую величину X. " +
		             "Продемонстрируйте выполнение запроса с различными X (подумайте, можно ли обойтись без копирования запроса несколько раз) ")]
		public void Linq1()
		{
			foreach (decimal x in new[] {20000m, 30000m, 100000m})
			{
				var customers = from customer in dataSource.Customers
								where customer.Orders.Sum(order => order.Total) > x
								select customer;

				Console.WriteLine($"{x}:");
				DumpCollection(customers);
			}
		}

		[Category("Tasks")]
		[Title("2.1")]
		[Description("Для каждого клиента составьте список поставщиков, находящихся в той же стране и том же городе. С группировкой.")]
		public void Linq21()
		{
			var result = from customer in dataSource.Customers
						 select new
						 {
							 Customer = customer,
							 Suppliers = from supplier in dataSource.Suppliers
										 group supplier by new { supplier.Country, supplier.City } into suppliers
										 where suppliers.Key.Equals(new { customer.Country, customer.City })
										 select suppliers
						 };
							

			foreach (var c in result)
			{
				ObjectDumper.Write(c.Customer);

				foreach (var s in c.Suppliers)
					ObjectDumper.Write(s);
			}
		}

		[Category("Tasks")]
		[Title("2.2")]
		[Description("Для каждого клиента составьте список поставщиков, находящихся в той же стране и том же городе. Без группировки.")]
		public void Linq22()
		{
			var results = from customer in dataSource.Customers
							select new
								{
									Customer = customer,
									Suppliers = from supplier in dataSource.Suppliers
									where customer.Country == supplier.Country && customer.City == supplier.City
									select supplier
								};

			foreach (var result in results)
			{
				ObjectDumper.Write(result.Customer);
				DumpCollection(result.Suppliers);
			}
		}

		[Category("Tasks")]
		[Title("3")]
		[Description("Найдите всех клиентов, у которых были заказы, превосходящие по сумме величину X")]
		public void Linq3()
		{
			var customers = from customer in dataSource.Customers
							where customer.Orders.Any(o => o.Total > 300)
							select customer;

			DumpCollection(customers);
		}

		[Category("Tasks")]
		[Title("4")]
		[Description("Выдайте список клиентов с указанием, начиная с какого месяца какого года они стали клиентами " +
		             "(принять за таковые месяц и год самого первого заказа) ")]
		public void Linq4()
		{
			var customers = from customer in dataSource.Customers
							let firsOrder = customer.Orders.OrderBy(o => o.OrderDate).FirstOrDefault()
							select new { customer.CompanyName, firsOrder?.OrderDate.Month, firsOrder?.OrderDate.Year };

			DumpCollection(customers);
		}

		[Category("Tasks")]
		[Title("5")]
		[Description("Сделайте предыдущее задание, но выдайте список отсортированным по году, месяцу, " +
		             "оборотам клиента (от максимального к минимальному) и имени клиента")]
		public void Linq5()
		{
			var customers = from customer in dataSource.Customers
							let firsOrderDate = customer.Orders.OrderBy(o => o.OrderDate).FirstOrDefault()?.OrderDate ?? DateTime.MaxValue
							let sum = customer.Orders.Sum(o => o.Total)
							orderby firsOrderDate.Year, firsOrderDate.Month, sum descending, customer.CompanyName
							select new { customer.CompanyName, firsOrderDate.Year, firsOrderDate.Month, sum };

			foreach (var customer in customers)
			{
				Console.WriteLine($"{customer.Year,10}{customer.Month,5}{customer.sum,20}    {customer.CompanyName}");
			}
		}

		[Category("Tasks")]
		[Title("6")]
		[Description("Укажите всех клиентов, у которых указан нецифровой код или не заполнен регион " +
		             "или в телефоне не указан код оператора (для простоты считаем, что это равнозначно «нет круглых скобочек в начале»).")]
		public void Linq6()
		{
			var numcode = new Regex("^[0-9]+$");

			var customers = from customer in dataSource.Customers
							where (!string.IsNullOrEmpty(customer.PostalCode) && !numcode.IsMatch(customer.PostalCode)) ||
								  string.IsNullOrEmpty(customer.Region) || !customer.Phone.StartsWith("(")
							select new { customer.CompanyName, customer.PostalCode, customer.Region, customer.Phone };

			foreach (var customer in customers)
			{
				Console.WriteLine($"{customer.CompanyName}  {customer.PostalCode}  {string.IsNullOrEmpty(customer.Region)}  {customer.Phone}");
			}
		}

		[Category("Tasks")]
		[Title("7")]
		[Description("Сгруппируйте все продукты по категориям, внутри – по наличию на складе, внутри последней группы отсортируйте по стоимости")]
		public void Linq7()
		{
			var products = from product in dataSource.Products
						   group product by product.Category into categories
						   select new
						   {
							   categories.Key,
							   Groups = from category in categories
										group category by category.UnitsInStock > 0 into subgroup
										select new
										{
											subgroup.Key,
											Subgroups = from gr in subgroup
														orderby gr.UnitPrice
														select gr
										}
						   };

			foreach (var group in products)
			{
				Console.WriteLine($"{group.Key}");
				foreach (var subgr in group.Groups)
				{
					Console.WriteLine($"    {subgr.Key}");
					foreach (var product in subgr.Subgroups)
					{
						Console.WriteLine($"        {product.UnitPrice}");
					}
				}
			}
		}

		[Category("Tasks")]
		[Title("8")]
		[Description("Сгруппируйте товары по группам «дешевые», «средняя цена», «дорогие». Границы каждой группы задайте сами")]
		public void Linq8()
		{
			decimal low = 10;
			decimal hight = 100;

			var products = from product in dataSource.Products
						   group product by new
						   {
							   Category = new Func<int>(() =>
							   {
								   if (product.UnitPrice <= low) return 0;
								   if (product.UnitPrice > low && product.UnitPrice <= hight) return 1;
								   return 2;
							   })()
						   };

			foreach (var group in products.OrderBy(gr => gr.Key.Category))
			{
				Console.WriteLine("-------------------");
				foreach (var product in group)
					Console.WriteLine($"    {product.UnitPrice}");
			}
		}

		[Category("Tasks")]
		[Title("9")]
		[Description("Рассчитайте среднюю прибыльность каждого города (среднюю сумму заказа по всем клиентам из данного города)" +
		             " и среднюю интенсивность (среднее количество заказов, приходящееся на клиента из каждого города)")]
		public void Linq9()
		{
			var cities =
							from customer in dataSource.Customers
							group customer by customer.City into city
							orderby city.Key
							select new
							{
								City = city.Key,
								AverageSum = city.Average(c => c.Orders.Sum(o => o.Total)),
								AverageCount = city.Average(c => c.Orders.Length)
							};

			foreach (var city in cities)
			{
				Console.WriteLine($"{city.City,20}:   {city.AverageSum,10:F2} | {city.AverageCount:F0}");
			}
		}

		[Category("Tasks")]
		[Title("10")]
		[Description("Сделайте среднегодовую статистику активности клиентов по месяцам (без учета года), " +
		             "статистику по годам, по годам и месяцам (т.е. когда один месяц в разные годы имеет своё значение).")]
		public void Linq10()
		{
			var cities =
							from customer in dataSource.Customers
							group customer by customer.City into city
							orderby city.Key
							select new
							{
								City = city.Key,
								AverageSum = city.Average(c => c.Orders.Sum(o => o.Total)),
								AverageCount = city.Average(c => c.Orders.Length)
							};

			foreach (var city in cities)
			{
				Console.WriteLine($"{city.City,20}:   {city.AverageSum,10:F2} | {city.AverageCount:F0}");
			}
		}

		private void DumpCollection(IEnumerable<object> collection)
		{
			foreach (var obj in collection)
			{
				ObjectDumper.Write(obj);
			}
		}
	}
}
