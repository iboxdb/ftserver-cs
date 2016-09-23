## Lightweight Full Text Search Server for CSharp

### Setup

```
Download Project
cd FTServer
xsp4
```


![](https://github.com/iboxdb/ftserver/raw/master/FTServer/web/css/fts2.png)

### Dependencies
[iBoxDB](http://www.iboxdb.com/)

[CsQuery](https://github.com/jamietre/CsQuery)

[Semantic-UI](http://semantic-ui.com/)



### The results order
the results order based on the ID number in IndexTextNoTran(.. **long id**, ...),  descending order.

every page has two index-IDs, normal-id and rankup-id, the rankup-id is a big number and used to keep the important text on the **top**.  (the front results from SearchDistinct(IBox, String) )
````
Engine.IndexTextNoTran(..., p.Id, p.Content, ...);
Engine.IndexTextNoTran(..., p.RankUpId(), p.RankUpDescription(), ...);
````					

the RankUpId()
````
public long RankUpId()
{
    return id | (1L << 60);
}
````

if you have more more important text , you can add one more index-id
````
public long AdvertisingId()
{
    return id | (1L << 61);
}
````
````
public static long RankDownId(long id)
{
    return id & (~(1L << 60 & 1L << 61)) ;
}
public static bool IsAdvertisingId(long id)
{
    return id > (1L << 61) ;
}
````		


the Page.GetRandomContent() method is used to keep the Search-Page-Content always changing, doesn't affect the real page order.


#### the Page and the Index -Process flow

When Insert

1.insert page --> 2.insert index
````
DB.Insert ("Page", page);
Engine.IndexTextNoTran( IsRemove = false );
...IndexTextNoTran...
````


When Delete  

1.delete index --> 2.delete page
````
Engine.IndexTextNoTran( IsRemove = true );
...IndexTextNoTran...
DB.Delete("Page", page.Id);
````				



