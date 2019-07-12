﻿using System;
using GeoUK.Coordinates;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using GeoUK.Ellipsoids;
using GeoUK.Projections;

namespace GeoUK
{
	public static class OSTN02Transform
	{
		/// <summary>
		/// Performs an ETRS89 to OSGB36/ODN datum transformation. Accuracy is approximately 10 centimeters. 
		/// Whilst very accurate this method is much slower than the Helmert transformation.
		/// </summary>
		public static Osgb36 Etrs89ToOsgb(LatitudeLongitude coordinates)
		{
			var enCoordinates = Convert.ToEastingNorthing (new Grs80 (), new BritishNationalGrid (), coordinates);
			return Etrs89ToOsgb (enCoordinates, coordinates.ElipsoidalHeight);
		}

		private static Osgb36 Etrs89ToOsgb(EastingNorthing coordinates, double ellipsoidHeight)
		{
			var dataStream = GetEmbeddedOSTN02 ();

			TextReader tr = new StreamReader(dataStream);
			List<int> recordNumbers = new List<int>();
			string[] records = new string[4];

			//determine record numbers
			int eastIndex = (int)(coordinates.Easting / 1000.0);
			int northIndex = (int)(coordinates.Northing / 1000.0);

			double x0 = eastIndex * 1000;
			double y0 = northIndex * 1000;

			//work out the four records
			recordNumbers.Add(CalculateRecordNumber(eastIndex, northIndex));
			recordNumbers.Add(CalculateRecordNumber(eastIndex + 1, northIndex));
			recordNumbers.Add(CalculateRecordNumber(eastIndex + 1, northIndex + 1));
			recordNumbers.Add(CalculateRecordNumber(eastIndex, northIndex + 1));

			//get records from the data file
			int recordsFound = 0;
			while (recordsFound < 4)
			{
				string csvRecord = tr.ReadLine();
				for (int index = 0; index < 4; index++)
				{
					if (csvRecord.StartsWith (recordNumbers [index].ToString ().Trim () + ",", StringComparison.Ordinal)) {
						//dont use add as we need to keep these in same order as record numbers
						records [index] = csvRecord;
						recordsFound++;
					}
				}
			}

			//populate the properties
			string[] fields0 = records[0].Split(",".ToCharArray());
			string[] fields1 = records[1].Split(",".ToCharArray());
			string[] fields2 = records[2].Split(",".ToCharArray());
			string[] fields3 = records[3].Split(",".ToCharArray());

			var se0 = System.Convert.ToDouble(fields0[3]);
			var se1 = System.Convert.ToDouble(fields1[3]);
			var se2 = System.Convert.ToDouble(fields2[3]);
			var se3 = System.Convert.ToDouble(fields3[3]);

			var sn0 = System.Convert.ToDouble(fields0[4]);
			var sn1 = System.Convert.ToDouble(fields1[4]);
			var sn2 = System.Convert.ToDouble(fields2[4]);
			var sn3 = System.Convert.ToDouble(fields3[4]);

			var sg0 = System.Convert.ToDouble(fields0[5]);
			var sg1 = System.Convert.ToDouble(fields1[5]);
			var sg2 = System.Convert.ToDouble(fields2[5]);
			var sg3 = System.Convert.ToDouble(fields3[5]);

			var dx = coordinates.Easting - x0;
			var dy = coordinates.Northing - y0;

			var t = dx / 1000.0;
			var u = dy / 1000.0;

			var se = (1 - t) * (1 - u) * se0 + t * (1 - u) * se1 + t * u * se2 + (1 - t) * u * se3;
			var sn = (1 - t) * (1 - u) * sn0 + t * (1 - u) * sn1 + t * u * sn2 + (1 - t) * u * sn3;
			var sg = (1 - t) * (1 - u) * sg0 + t * (1 - u) * sg1 + t * u * sg2 + (1 - t) * u * sg3;

			var geoidDatum = (Osgb36GeoidDatum)System.Convert.ToInt32(fields0[6]);

			var easting = coordinates.Easting + se;
			var northing = coordinates.Northing + sn;
			var height = ellipsoidHeight - sg;

			return new Osgb36(easting, northing, height, geoidDatum);

		}

		/// <summary>
		/// Calculates a data file record number.
		/// </summary>
		/// <param name="eastIndex"></param>
		/// <param name="northIndex"></param>
		/// <returns></returns>
		private static int CalculateRecordNumber(int eastIndex, int northIndex)
		{
			return eastIndex + (northIndex * 701) + 1;
		}
		private static Stream GetEmbeddedOSTN02()
		{
			return ResourceManager.GetEmbeddedResourceStream (typeof(OSTN02Transform).GetTypeInfo ().Assembly, "OSTN02_OSGM02_GB.txt");
		}
	}
}

