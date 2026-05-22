importScripts('https://www.gstatic.com/firebasejs/10.7.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.7.0/firebase-messaging-compat.js');

firebase.initializeApp({
  apiKey: "AIzaSyD9cZOHciYOa07oI3xWa6PyJc5K7Uw_quo",
  authDomain: "kms-app-6af5c.firebaseapp.com",
  projectId: "kms-app-6af5c",
  storageBucket: "kms-app-6af5c.firebasestorage.app",
  messagingSenderId: "511916590161",
  appId: "1:511916590161:web:dcc1ee470a55d931306470"
});

const messaging = firebase.messaging();

messaging.onBackgroundMessage(function(payload) {
  console.log('Background message:', payload);
  const title = payload.notification?.title || 'KMS';
  const body  = payload.notification?.body  || '';
  self.registration.showNotification(title, {
    body: body,
    icon: '/icon-192.png',
    badge: '/icon-192.png'
  });
});
