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
    [Collection("Sequential")] // run tests sequentially because we are dealing with file system
    public class GlobalTests : TestBase
    {
        private readonly InvoiceProvider InvoiceProvider = new();

        [Fact]
        public void TestAutomaticLineIds()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.TradeLineItems.Clear();

            desc.AddTradeLineItem("Item1");
            desc.AddTradeLineItem("Item2");

            desc.TradeLineItems[0].AssociatedDocument.LineID.Should().Be("1");
            desc.TradeLineItems[1].AssociatedDocument.LineID.Should().Be("2");
        } // !TestAutomaticLineIds()



        [Fact]
        public void TestManualLineIds()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.TradeLineItems.Clear();
            desc.AddTradeLineItem(lineID: "item-01", "Item1");
            desc.AddTradeLineItem(lineID: "item-02", "Item2");

            desc.TradeLineItems[0].AssociatedDocument.LineID.Should().Be("item-01");
            desc.TradeLineItems[1].AssociatedDocument.LineID.Should().Be("item-02");
        } // !TestManualLineIds()


        [Fact]
        public void TestCommentLine()
        {
            var COMMENT = Guid.NewGuid().ToString();
            var CUSTOM_LINE_ID = Guid.NewGuid().ToString();

            // test with automatic line id
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var numberOfTradeLineItems = desc.TradeLineItems.Count;
            desc.AddTradeLineCommentItem(COMMENT);

            numberOfTradeLineItems.Should().Be(desc.TradeLineItems.Count - 1);
            desc.TradeLineItems[^1].AssociatedDocument.Should().NotBeNull();
            desc.TradeLineItems[^1].AssociatedDocument.Notes.Should().NotBeNull();
            desc.TradeLineItems[^1].AssociatedDocument.Notes.Count.Should().Be(1);
            desc.TradeLineItems[^1].AssociatedDocument.Notes[0].Content.Should().Be(COMMENT);

            // test with manual line id
            desc = InvoiceProvider.CreateInvoice();
            numberOfTradeLineItems = desc.TradeLineItems.Count;
            desc.AddTradeLineCommentItem(lineID: CUSTOM_LINE_ID, comment: COMMENT);

            numberOfTradeLineItems.Should().Be(desc.TradeLineItems.Count - 1);
            desc.TradeLineItems[^1].AssociatedDocument.Should().NotBeNull();
            desc.TradeLineItems[^1].AssociatedDocument.LineID.Should().Be(CUSTOM_LINE_ID);
            desc.TradeLineItems[^1].AssociatedDocument.Notes.Should().NotBeNull();
            desc.TradeLineItems[^1].AssociatedDocument.Notes.Count.Should().Be(1);
            desc.TradeLineItems[^1].AssociatedDocument.Notes[0].Content.Should().Be(COMMENT);
        } // !TestCommentLine()

        [Fact]
        public void TestGetVersion()
        {
            var path = @"..\..\..\..\demodata\zugferd10\ZUGFeRD_1p0_COMFORT_Einfach.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);
            InvoiceDescriptor.GetVersion(path).Should().Be(ZUGFeRDVersion.Version1);

            path = @"..\..\..\..\demodata\zugferd20\zugferd_2p0_BASIC_Einfach.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);
            InvoiceDescriptor.GetVersion(path).Should().Be(ZUGFeRDVersion.Version20);

            path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_BASIC_Einfach-factur-x.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);
            InvoiceDescriptor.GetVersion(path).Should().Be(ZUGFeRDVersion.Version23);
        } // !TestGetVersion()


        [Theory]
        [InlineData(ZUGFeRDVersion.Version1, Profile.Extended)]
        [InlineData(ZUGFeRDVersion.Version1, Profile.XRechnung)]
        [InlineData(ZUGFeRDVersion.Version20, Profile.Extended)]
        [InlineData(ZUGFeRDVersion.Version20, Profile.XRechnung)]
        [InlineData(ZUGFeRDVersion.Version20, Profile.XRechnung1)]
        [InlineData(ZUGFeRDVersion.Version23, Profile.Extended)]
        [InlineData(ZUGFeRDVersion.Version23, Profile.XRechnung1)]
        public void UBLNonAvailability(ZUGFeRDVersion version, Profile profile)
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            using var ms = new MemoryStream();
            desc.Invoking(d => d.Save(ms, version, profile, ZUGFeRDFormats.UBL))
                .Should().Throw<UnsupportedException>();
        } // !UBLNonAvailability()


        [Fact]
        public void UBLAvailability()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            using var ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
        } // !UBLAvailability()


        [Theory]
        [InlineData(ZUGFeRDVersion.Version1, Profile.Extended)]
        [InlineData(ZUGFeRDVersion.Version20, Profile.Extended)]
        [InlineData(ZUGFeRDVersion.Version23, Profile.Extended)]
        public void SavingThenReadingAppliedTradeTaxes(ZUGFeRDVersion version, Profile profile)
        {
            var expected = InvoiceDescriptor.CreateInvoice("123", new DateTime(2024, 12, 5), CurrencyCodes.EUR);
            var lineItem = expected.AddTradeLineItem(name: "Something",
                grossUnitPrice: 9.9m,
                netUnitPrice: 9.9m,
                billedQuantity: 20m,
                taxType: TaxTypes.VAT,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19m
                );
            lineItem.LineTotalAmount = 198m; // 20 * 9.9
            expected.AddApplicableTradeTax(
                basisAmount: lineItem.LineTotalAmount!.Value,
                percent: 19m,
                taxAmount: 29.82m, // 19% of 198
                typeCode: TaxTypes.VAT,
                categoryCode: TaxCategoryCodes.S,
                allowanceChargeBasisAmount: -5m,
                lineTotalBasisAmount: lineItem.LineTotalAmount!.Value
                );
            expected.LineTotalAmount = 198m;
            expected.TaxBasisAmount = 198m;
            expected.TaxTotalAmount = 29.82m;
            expected.GrandTotalAmount = 198m + 29.82m;
            expected.DuePayableAmount = expected.GrandTotalAmount;

            using MemoryStream ms = new();
            expected.Save(ms, version, profile);
            ms.Seek(0, SeekOrigin.Begin);

            var actual = InvoiceDescriptor.Load(ms);

            actual.Taxes.Count.Should().Be(expected.Taxes.Count);
            actual.Taxes.Count.Should().Be(1);
            Tax actualTax = actual.Taxes[0];
            actualTax.BasisAmount.Should().Be(198m);
            actualTax.Percent.Should().Be(19m);
            actualTax.TaxAmount.Should().Be(29.82m);
            actualTax.TypeCode.Should().Be(TaxTypes.VAT);
            actualTax.CategoryCode.Should().Be(TaxCategoryCodes.S);
            actualTax.AllowanceChargeBasisAmount.Should().Be(-5m);
            actualTax.LineTotalBasisAmount.Should().Be(198m);
        } // !SavingThenReadingAppliedTradeTaxes()
    }
}
