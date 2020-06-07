## Lightweight Full Text Search Server for CSharp

### Setup

```s
Download Project / git clone https://github.com/iboxdb/ftserver-cs.git
cd FTServer
dotnet run -c Release
```


![](https://github.com/iboxdb/ftserver/raw/master/FTServer/web/css/fts2.png)

### Dependencies
[iBoxDB](http://www.iboxdb.com/)

[AngleSharp](https://github.com/AngleSharp/AngleSharp)

[Semantic-UI](http://semantic-ui.com/)


### The Results Order
The results order based on the **id()** number in **class PageText**,  descending order.

Page has many PageTexts.



the Page.GetRandomContent() method is used to keep the Search-Page-Content always changing, doesn't affect the real PageText order.

if you have many pages(>100,000),  use the ID number to control the order instead of loading all pages to memory. 
Or you can load top 100-1000 pages to memory then re-order it by favor. 


#### Search Format

[Word1 Word2 Word3] => text has **Word1** and **Word2** and **Word3**

["Word1 Word2 Word3"] => text has **"Word1 Word2 Word3"** as a whole

Search [https http] => get almost all pages

#### Search Method
search (... String keywords, long **startId**, long **count**)

**startId** => which ID(the id when you created PageText) to start, 
use (startId=Long.MaxValue) to read from the top, descending order

**count** => records to read,  **important parameter**, the search speed depends on this parameter, not how big the data is.

##### Next Page
set the startId as the last id from the results of search minus one

```java
startId = search( "keywords", startId, count);
nextpage_startId = startId - 1 // this 'minus one' has done inside search()
...
//read next page
search("keywords", nextpage_startId, count)
```

mostly, the nextpage_startId is posted from client browser when user reached the end of webpage, 
and set the default nextpage_startId=Long.MaxValue, 
in javascript the big number have to write as String ("'" + nextpage_startId + "'")


#### The Page-Text and the Text-Index -Process flow

When Insert

1.insert page --> 2.insert index
````cs
IndexAPI.addPage(p);
IndexAPI.addPageIndex(url);
````


When Delete

1.delete index --> 2.delete page
````cs
IndexAPI.removePage(url);
````

#### Memory
````cs
//Bigger, faster, more memories.
//Smaller, less memory.
int PageText.max_text_length ;
````

#### Add Custom information to Page
```cs
IndexPage.addPageCustomText(furl, ttitle, tmsg);
```

#### Private Server
Open 
```cs
public Page Html.Get(String url);
```
Set your private WebSite text
```cs
Page page = new Page();
page.url = url;
page.title = title;
page.text = bodyText
page... = ...
return page;
```


#### How to set big cache
```cs
DatabaseConfig dbcfg = db.GetConfig(); 
dbcfg.CacheLength = dbcfg.MB(2048);
//Or
dbcfg.CacheLength = 2048L * 1024L * 1024L;

//Wrong, overflow
//dbcfg.CacheLength = 2048 * 1024 * 1024;
```



#### Tools
Linux + Visual Studio Code + ASP.NET Core

#### More
[Transplant from Full Text Search Java JSP Version](https://github.com/iboxdb/ftserver)
