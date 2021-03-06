﻿// FullProtocolTests.cs
//
// Author:
//       Ricky Curtice <ricky@rwcproductions.com>
//
// Copyright (c) 2017 Richard Curtice
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Threading;
using InWorldz.Whip.Client;
using NUnit.Framework;

namespace UnitTests.WHIPTests {
	[TestFixture]
	public static class FullProtocolTests {
		private static Socket _socket;

		[OneTimeSetUp]
		public static void Setup() {
			_socket = AuthTests.Connect();
		}

		[OneTimeTearDown]
		public static void Teardown() {
			_socket.Dispose();
			_socket = null;
		}

		#region GET

		[Test]
		[Timeout(60000)]
		public static void TestOperationGETWithInvalidIdReturnsError() {
			var assetId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.GET, assetId);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.ERROR, response.Status, $"Wrong result returned for asset {assetId}.");
		}

		[Test]
		[Timeout(60000)]
		public static void TestOperationGETWithRandomIdReturnsNotFound() {
			var assetId = Guid.NewGuid().ToString("N");

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.GET, assetId);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.NOT_FOUND, response.Status, $"Wrong result returned for asset {assetId}.");
		}

		[Test]
		[Timeout(60000)]
		public static void TestOperationGETWithValidIdReturnsCorrectAsset() {
			var asset = CreateAndPutAsset(_socket);
			Assert.NotNull(asset, "Failure storing asset for test.");

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.GET, asset.Uuid);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			var resultingAsset = new Asset(response.Data);

			Assert.AreEqual(asset.Uuid, resultingAsset.Uuid, "Asset ID fails to match.");
			Assert.AreEqual(asset.Type, resultingAsset.Type, "Asset Type fails to match.");
			Assert.AreEqual(asset.Local, resultingAsset.Local, "Asset Local flag fails to match.");
			Assert.AreEqual(asset.Temporary, resultingAsset.Temporary, "Asset Temporary flag fails to match.");
			Assert.AreEqual(asset.CreateTime, resultingAsset.CreateTime, "Asset CreateTime fails to match.");
			Assert.AreEqual(asset.Name, resultingAsset.Name, "Asset Name fails to match.");
			Assert.AreEqual(asset.Description, resultingAsset.Description, "Asset Description fails to match.");
			Assert.AreEqual(asset.Data, resultingAsset.Data, "Asset Data fails to match.");
		}

		[Test]
		[Timeout(60000)]
		public static void TestOperationGETWithValidIdReturnsFound() {
			var asset = CreateAndPutAsset(_socket);
			Assert.NotNull(asset, "Failure storing asset for test.");

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.GET, asset.Uuid);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			Thread.Sleep(10);
			Assert.NotZero(_socket.Available, "Got nothing from the server!");

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.FOUND, response.Status, $"Wrong result returned for asset {asset.Uuid}.");
		}

		#endregion

		#region GET_DONTCACHE

		[Test]
		[Timeout(60000)]
		public static void TestOperationGET_DONTCACHEWithValidIdReturnsOk() {
			var asset = CreateAndPutAsset(_socket);
			Assert.NotNull(asset, "Failure storing asset for test.");

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.GET_DONTCACHE, asset.Uuid);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.FOUND, response.Status, $"Wrong result returned for asset {asset.Uuid}.");
		}

		#endregion

		#region MAINT_PURGELOCALS

		[Test]
		[Timeout(60000)]
		public static void TestOperationMAINT_PURGELOCALSReturnsOk() {
			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.MAINT_PURGELOCALS, Guid.Empty.ToString("N"));
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.OK, response.Status, $"Wrong result returned for purge locals.");
		}

		#endregion

		#region PURGE

		[Test]
		[Timeout(60000)]
		public static void TestOperationPURGEWithInvalidIdReturnsError() {
			var assetId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.PURGE, assetId);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.ERROR, response.Status, $"Wrong result returned for asset {assetId}.");
		}

		[Test]
		[Timeout(60000)]
		public static void TestOperationPURGEWithRandomIdReturnsNotFound() {
			var assetId = Guid.NewGuid().ToString("N");

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.PURGE, assetId);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.NOT_FOUND, response.Status, $"Wrong result returned for asset {assetId}.");
		}

		[Test]
		[Timeout(60000)]
		public static void TestOperationPURGEWithValidIdReturnsOk() {
			var asset = CreateAndPutAsset(_socket);
			Assert.NotNull(asset, "Failure putting asset while prepping for test.");

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.PURGE, asset.Uuid);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.OK, response.Status, $"Wrong result returned for asset {asset.Uuid}.");
		}

		[Test]
		[Timeout(60000)]
		public static void TestOperationPURGEWithValidIdRemovedItem() {
			var asset = CreateAndPutAsset(_socket);
			Assert.NotNull(asset, "Failure putting asset while prepping for test.");

			var purgeRequest = new ClientRequestMsg(ClientRequestMsg.RequestType.PURGE, asset.Uuid);
			purgeRequest.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var purgeResponse = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.OK, purgeResponse.Status, $"Wrong result returned for asset {asset.Uuid}.");

			var getRequest = new ClientRequestMsg(ClientRequestMsg.RequestType.GET, asset.Uuid);
			getRequest.Send(_socket);

			Thread.Sleep(10);
			Assert.NotZero(_socket.Available, "Got nothing from the server!");

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.NOT_FOUND, response.Status, $"Wrong result returned for asset {asset.Uuid}.");
		}

		#endregion

		#region PUT

		[Test]
		[Timeout(60000)]
		public static void TestOperationPUTWithDuplicateIdReturnsError() {
			var asset = CreateAndPutAsset(_socket);
			Assert.NotNull(asset, "Failure putting asset while prepping for test.");

			var data = asset.Serialize().data;

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.PUT, asset.Uuid, data);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.ERROR, response.Status, $"Wrong result returned for asset {asset.Uuid}.");
		}

		[Test]
		[Timeout(60000)]
		public static void TestOperationPUTWithValidIdReturnsOk() {
			var asset = new Asset(
				Guid.NewGuid().ToString("N"),
				7, // Notecard
				false,
				false,
				(int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds,
				"Just junk",
				"Just junk.",
				new byte[] { 0x31, 0x33, 0x33, 0x37 }
			);

			var data = asset.Serialize().data;

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.PUT, asset.Uuid, data);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.OK, response.Status, $"Wrong result returned for asset {asset.Uuid}.");
		}

		#endregion

		#region STATUS_GET

		[Test]
		[Timeout(60000)]
		public static void TestOperationSTATUS_GETHasExpectedValue() {
			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.STATUS_GET, Guid.Empty.ToString("N"));
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);

			var statusInfo = response.ErrorMessage;
			Assert.That(statusInfo.StartsWith("WHIP Server Status", StringComparison.InvariantCulture), $"Expected to find 'WHIP Server Status' in the following:\n{statusInfo}");
		}

		[Test]
		[Timeout(60000)]
		public static void TestOperationSTATUS_GETReturnsOk() {
			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.STATUS_GET, Guid.Empty.ToString("N"));
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.OK, response.Status, $"Wrong result returned.");
		}

		#endregion

		#region STORED_ASSET_IDS_GET

		[Test]
		[Timeout(60000)]
		public static void TestOperationSTORED_ASSET_IDS_GETHasExpectedEntry() {
			var asset = CreateAndPutAsset(_socket);
			Assert.NotNull(asset, "Failure putting asset while prepping for test.");

			var assetRangeId = asset.Uuid.Substring(0, 3) + Guid.Empty.ToString("N").Substring(3);

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.STORED_ASSET_IDS_GET, assetRangeId);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);

			var assetIdCSV = response.ErrorMessage;
			Assert.That(assetIdCSV.Contains(asset.Uuid), $"Expected to find '{asset.Uuid}' in the following:\n{assetIdCSV}");
		}

		[Test]
		[Timeout(60000)]
		public static void TestOperationSTORED_ASSET_IDS_GETReturnsOk() {
			var asset = CreateAndPutAsset(_socket);
			Assert.NotNull(asset, "Failure putting asset while prepping for test.");

			var assetRangeId = asset.Uuid.Substring(0, 3) + Guid.Empty.ToString("N").Substring(3);

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.STORED_ASSET_IDS_GET, assetRangeId);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.OK, response.Status, $"Wrong result returned for asset range matching {assetRangeId.Substring(0, 3)}*.");
		}

		#endregion

		#region TEST

		[Test]
		[Timeout(60000)]
		public static void TestOperationTESTWithInvalidIdReturnsError() {
			var assetId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.TEST, assetId);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.ERROR, response.Status, $"Wrong result returned for asset {assetId}.");
		}

		[Test]
		[Timeout(60000)]
		public static void TestOperationTESTWithRandomIdReturnsNotFound() {
			var assetId = Guid.NewGuid().ToString("N");

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.TEST, assetId);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.NOT_FOUND, response.Status, $"Wrong result returned for asset {assetId}.");
		}

		[Test]
		[Timeout(60000)]
		public static void TestOperationTESTWithValidIdReturnsFound() {
			var asset = CreateAndPutAsset(_socket);
			Assert.NotNull(asset, "Failure putting asset while prepping for test.");

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.TEST, asset.Uuid);
			request.Send(_socket);

			while (_socket.Available <= 0) {
			}

			var response = new ServerResponseMsg(_socket);
			Assert.AreEqual(ServerResponseMsg.Result.FOUND, response.Status, $"Wrong result returned for asset {asset.Uuid}.");
		}

		#endregion

		public static Asset CreateAndPutAsset(Socket conn, byte[] assetData = null) {
			Contract.Requires(conn != null);

			var asset = new Asset(
				Guid.NewGuid().ToString("N"),
				7, // Notecard
				false,
				false,
				(int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds,
				"Just junk",
				"Just junk.",
				assetData ?? new byte[] { 0x31, 0x33, 0x33, 0x37 }
			);

			var data = asset.Serialize().data;

			var request = new ClientRequestMsg(ClientRequestMsg.RequestType.PUT, asset.Uuid, data);
			request.Send(conn);

			while (conn.Available <= 0) {
			}

			var response = new ServerResponseMsg(conn);

			return response.Status == ServerResponseMsg.Result.OK ? asset : null;
		}
	}
}
