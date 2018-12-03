
GET /offentliggoerelser/_search 
{    "query": {
        "bool": {
            "must": [
                {
                    "range": {
                        "offentliggoerelsesTidspunkt": {
                            "gte": "2017-11-29T00:00:00.000Z",
                            "lte": "2017-12-02T00:00:00.000Z"
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