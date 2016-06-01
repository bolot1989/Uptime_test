using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using WebApplication3.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WebApplication3.Controllers
{
    public class HomeController : Controller
    {
        private const string MY_AWS_ACCESS_KEY_ID = "AKIAJJBZBUKPCMOAI67A";
        private const string MY_AWS_SECRET_KEY = "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";
        private const string DESTINATION = "ecs.amazonaws.com";

        private const string NAMESPACE = "http://webservices.amazon.com/AWSECommerceService/2016-05-19";
        private const string ITEM_ID = "0545010225";
        private List<Book> listOfBooks = new List<Book>();
        private string buff = null;

        // GET: Home
        public ActionResult Index(string searchProduct)
        {
            if (!String.IsNullOrEmpty(searchProduct))
            {
                String requestUrl;
                SignedRequestHelper helper = new SignedRequestHelper(MY_AWS_ACCESS_KEY_ID, MY_AWS_SECRET_KEY, DESTINATION);

                for (int p = 1; p <= 1; p++)
                {
                    String requestString = "Service=AWSECommerceService"
                        + "&Operation=ItemSearch"
                        + "&SearchIndex=Books"
                        + "&ResponseGroup=Small,Offers"
                        + "&ItemPage=" + p
                        + "&AssociateTag=bolot1989-20"
                        + "&Keywords=" + searchProduct;
                    requestUrl = helper.Sign(requestString);

                    WebRequest request = HttpWebRequest.Create(requestUrl);
                    WebResponse response = request.GetResponse();
                    XmlDocument doc = new XmlDocument();
                    doc.Load(response.GetResponseStream());

                    XmlNodeList errorMessageNodes = doc.GetElementsByTagName("Message", NAMESPACE);
                    if (errorMessageNodes != null && errorMessageNodes.Count > 0)
                    {
                        String message = errorMessageNodes.Item(0).InnerText;
                        ViewBag.Keyword += "Error: " + message + " (but signature worked)";
                    }

                    String data = "", titleString = "", authorString = "", priceString = "";
                    XmlNodeList childNode, itemAttributesList;
                    XmlNodeList itemsList = doc.GetElementsByTagName("Item");
                    for (int i = 0; i < itemsList.Count; i++)
                    {
                        childNode = itemsList[i].ChildNodes;
                        for (int j = 0; j < childNode.Count; j++)
                        {
                            if (childNode[j].Name == "ItemAttributes")
                            {
                                itemAttributesList = childNode[j].ChildNodes;
                                for (int k = itemAttributesList.Count - 1; k > -1; k--)
                                {
                                    if (itemAttributesList[k].Name == "Title")
                                    {
                                        titleString += itemAttributesList[k].InnerText;
                                    }
                                    if (itemAttributesList[k].Name == "Author")
                                    {
                                        authorString += itemAttributesList[k].InnerText;
                                    }
                                }
                            }
                            if (childNode[j].Name == "OfferSummary")
                            {
                                priceString += childNode[j].FirstChild.LastChild.InnerText;
                            }
                        }
                        listOfBooks.Add(new Book { Name = titleString, Authors = authorString, Price = priceString });
                        titleString = null;
                        authorString = null;
                        priceString = null;
                    }
                }
            }
            return View(listOfBooks);
        }

        public ActionResult Search(string searchProduct)
        {
            return View();
        }

       public ActionResult MoveRequest(int item, string searchProduct)
       {
            listOfBooks = new List<Book>();
            String requestUrl;
            SignedRequestHelper helper = new SignedRequestHelper(MY_AWS_ACCESS_KEY_ID, MY_AWS_SECRET_KEY, DESTINATION);

            String requestString = "Service=AWSECommerceService"
                + "&Operation=ItemSearch"
                + "&SearchIndex=Books"
                + "&ResponseGroup=Small,Offers"
                + "&ItemPage=" + item.ToString()
                + "&AssociateTag=bolot1989-20"
                + "&Keywords=" + searchProduct;
            requestUrl = helper.Sign(requestString);
            
            WebRequest request = HttpWebRequest.Create(requestUrl);
            WebResponse response = request.GetResponse();
            XmlDocument doc = new XmlDocument();
            doc.Load(response.GetResponseStream());

            XmlNodeList errorMessageNodes = doc.GetElementsByTagName("Message", NAMESPACE);
            if (errorMessageNodes != null && errorMessageNodes.Count > 0)
            {
                String message = errorMessageNodes.Item(0).InnerText;
                ViewBag.Keyword += "Error: " + message + " (but signature worked)";
            }

            String data = "", titleString = "", authorString = "", priceString = "";
            XmlNodeList childNode, itemAttributesList;
            XmlNodeList itemsList = doc.GetElementsByTagName("Item");
            for (int i = 0; i < itemsList.Count; i++)
            {
                childNode = itemsList[i].ChildNodes;
                for (int j = 0; j < childNode.Count; j++)
                {
                    if (childNode[j].Name == "ItemAttributes")
                    {
                        itemAttributesList = childNode[j].ChildNodes;
                        for (int k = itemAttributesList.Count - 1; k > -1; k--)
                        {
                            if (itemAttributesList[k].Name == "Title")
                            {
                                titleString += itemAttributesList[k].InnerText;
                            }
                            if (itemAttributesList[k].Name == "Author")
                            {
                                authorString += itemAttributesList[k].InnerText;
                            }
                        }
                    }
                    if (childNode[j].Name == "OfferSummary")
                    {
                        priceString += childNode[j].FirstChild.LastChild.InnerText;
                    }
                }
                listOfBooks.Add(new Book { Name = titleString, Authors = authorString, Price = priceString });
                titleString = null;
                authorString = null;
                priceString = null;
            }
            

            return Json(listOfBooks, JsonRequestBehavior.AllowGet);
       }

        [HttpPost]
        public ActionResult ConvertCurrency(List<Book> books)
        {
            string ans = null;
            string toCurr = books[books.Count - 1].Price;
            string fromCurr = books[books.Count - 1].Name;
            books.RemoveAt(books.Count - 1);
            string numWithoutSign = null;
            decimal amount = 0;
            for (int i = 0; i < books.Count; i++)
            {
                if (books[i].Price != null)
                {
                    numWithoutSign = books[i].Price.Remove(0, 1);
                    amount = Decimal.Parse(numWithoutSign, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo);
                    books[i].Price = convertCurrency(amount, fromCurr, toCurr);
                }
            }
            return Json(books, JsonRequestBehavior.AllowGet);
        }

        public string convertCurrency(decimal amount, string fromCurrency, string toCurrency)
        {
            string apiURL = String.Format("https://www.google.com/finance/converter?a={0}&from={1}&to={2}&meta={3}", amount, fromCurrency, toCurrency, Guid.NewGuid().ToString());
            var request = WebRequest.Create(apiURL);
            var streamReader = new StreamReader(request.GetResponse().GetResponseStream(), System.Text.Encoding.ASCII);
            string result = "";
            try
            {
                result = Regex.Matches(streamReader.ReadToEnd(), "<span class=\"?bld\"?>([^<]+)</span>")[0].Groups[1].Value;
            }
            catch (ArgumentOutOfRangeException a)
            {
                result = null;
            }
            return result;
        }

    }
}