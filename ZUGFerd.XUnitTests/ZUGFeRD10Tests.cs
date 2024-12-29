/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
using FluentAssertions;

namespace s2industries.ZUGFeRD.Tests
{
    public class ZUGFeRD10Tests : TestBase
    {
        private readonly InvoiceProvider InvoiceProvider = new();

        [Fact]
        public void TestReferenceComfortInvoice()
        {
            var path = @"..\..\..\..\demodata\zugferd10\ZUGFeRD_1p0_COMFORT_Einfach.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);
            var desc = InvoiceDescriptor.Load(path);

            desc.Profile.Should().Be(Profile.Comfort);
            desc.Type.Should().Be(InvoiceType.Invoice);
            desc.IsTest.Should().BeTrue();
        } // !TestReferenceComfortInvoice()


        [Fact]
        public void TestReferenceComfortInvoiceRabattiert()
        {
            var path = @"..\..\..\..\demodata\zugferd10\ZUGFeRD_1p0_COMFORT_Rabatte.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var desc = InvoiceDescriptor.Load(path);

            desc.Save("test.xml", ZUGFeRDVersion.Version1, Profile.Comfort);

            desc.Profile.Should().Be(Profile.Comfort);
            desc.Type.Should().Be(InvoiceType.Invoice);
            desc.CreditorBankAccounts[0].BankName.Should().Be("Hausbank München");
        } // !TestReferenceComfortInvoiceRabattiert()


        [Fact]
        public void TestStoringInvoiceViaFile()
        {
            var path = "output.xml";
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.Save(path, ZUGFeRDVersion.Version1, Profile.Comfort);

            var desc2 = InvoiceDescriptor.Load(path);
            // TODO: Add more asserts
        } // !TestStoringInvoiceViaFile()


        [Fact]
        public void TestStoringInvoiceViaStreams()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();

            var path = "output_stream.xml";
            var saveStream = new FileStream(path, FileMode.Create);
            desc.Save(saveStream, ZUGFeRDVersion.Version1, Profile.Comfort);
            saveStream.Close();

            var loadStream = new FileStream(path, FileMode.Open);
            var desc2 = InvoiceDescriptor.Load(loadStream);
            loadStream.Close();

            desc2.Profile.Should().Be(Profile.Comfort);
            desc2.Type.Should().Be(InvoiceType.Invoice);

            // try again with a memory stream
            var ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version1, Profile.Comfort);

            var data = ms.ToArray();
            var s = System.Text.Encoding.Default.GetString(data);
            // TODO: Add more asserts
        } // !TestStoringInvoiceViaStream()


        [Fact]
        public void TestMissingPropertiesAreNull()
        {
            var path = @"..\..\..\..\demodata\zugferd10\ZUGFeRD_1p0_COMFORT_Einfach.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var invoiceDescriptor = InvoiceDescriptor.Load(path);

            invoiceDescriptor.TradeLineItems.Should().AllSatisfy(x =>
            {
                x.BillingPeriodStart.Should().BeNull();
                x.BillingPeriodEnd.Should().BeNull();
            });
        } // !TestMissingPropertiesAreNull()
    }
}
