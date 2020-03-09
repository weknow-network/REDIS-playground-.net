# READ ME
## Install local
```
kubectl create namespace redis
helm repo list
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update

# local
helm install redis bitnami/redis --set cluster.enabled=false --set usePassword=false --namespace redis
# cluster
helm install redis bitnami/redis --set cluster.enabled=true --set usePassword=false --namespace redis

kubectl port-forward --namespace redis svc/redis-master 6379:6379
```