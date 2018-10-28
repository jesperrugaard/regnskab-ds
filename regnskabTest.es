
GET /offentliggoerelser/_search 
{    "query": {
        "bool": {
            "must": [
                {
                    "range": {
                        "indlaesningsTidspunkt": {
                            "gte": "2018-09-24T00:00:00.000Z",
                            "lte": "2018-09-24T23:59:59.999Z"
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