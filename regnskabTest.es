
GET /offentliggoerelser/_search 
{    "query": {
        "bool": {
            "must": [
                {
                    "range": {
                        "indlaesningsTidspunkt": {
                            "gte": "2018-11-29T00:00:00.000Z",
                            "lte": "2018-12-02T00:00:00.000Z"
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