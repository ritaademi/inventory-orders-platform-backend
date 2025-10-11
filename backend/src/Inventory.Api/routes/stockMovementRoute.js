const express = require('express');
const router = express.Router();
const stockMovementController = require('../Controllers/stockMovementController');

// Rruga për krijimin e lëvizjes së stokut
router.post('/stockMovement', stockMovementController.createMovement);

module.exports = router;
