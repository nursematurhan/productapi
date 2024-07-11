curl "http://127.0.0.1:9080/apisix/admin/routes/1" -H "X-API-KEY: edd1c9f034335f136f87ad84b625c8f1" -X PUT -d '    
{
  "methods": ["GET"],                                       
  "uri": "/api/products",                   
  "upstream": {                           
    "type": "roundrobin",
    "nodes": {
      "productapi:80": 1
    }
  }
}'

#request to our target API /api/products

curl http://127.0.0.1:9080/api/products -i 