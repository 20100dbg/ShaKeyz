## Intro

ShaKeyz est un (tout) petit logiciel de partage de fichier en LAN.
Il s'agit davantage d'un snippet que d'un véritable logiciel, le but est de montrer comment effectuer une connexion en TCP et envoyer/recevoir des fichiers.

Pour le tester, lancez deux instances du logiciel (de préférence sur deux machines distinctes), l'une en serveur, l'autre en client.
Le client se connecte au serveur avec son IP/port.
Une fois la connexion établie, vous avez accès aux commandes search, send, request.

Exemple :

search *.jpg
renvoie une liste de fichier correspondant. * et ? fonctionnent

request unfichier.jpg
récupère un fichier

send unfichier.jpg
envoie le fichier



La gestion des erreurs est ici assez simpliste et il n'y a pas de vérification de bonne réception.


Détails :

- La connexion, en TCP, est établie avec les classes TcpListener et TcpClient.
- On récupère ensuite le NetworkStream associé pour transferer les données sérialisées avec un BinaryFormatter.
- Le thread principal permet de taper les commandes et envoie les données, un thread spécifique s'occupe de la réception des données.