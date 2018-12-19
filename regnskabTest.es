        
GET /offentliggoerelser/_search 
{    "query": {
        "bool": {
            "must": [
                {
                    "range": {
                        "offentliggoerelsesTidspunkt": {
                            "gte": "2015-06-03T00:00:00.000Z",
                            "lte": "2015-06-04T00:00:00.000Z"
                        }
                    }
                }
            ],
            "must_not": [ ],
            "should": [ ]
        }
    },
    "from": 0,
    "size": 3000,
    "sort": [ ],
    "aggs": { }
}