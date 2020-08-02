#!/bin/bash
gcloud builds submit --tag gcr.io/el-gato/templatizer --project el-gato
gcloud run deploy --image gcr.io/el-gato/templatizer --platform managed --project el-gato
